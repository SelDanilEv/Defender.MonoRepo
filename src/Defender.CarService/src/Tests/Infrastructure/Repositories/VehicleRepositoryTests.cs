using System.Reflection;
using Defender.CarService.Domain.Entities;
using Defender.CarService.Infrastructure.Persistence;
using Defender.CarService.Infrastructure.Repositories;
using Moq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Defender.CarService.Tests.Infrastructure.Repositories;

public sealed class VehicleRepositoryTests
{
    [Fact]
    public async Task ReplaceAsync_WhenExpectedVersionDoesNotMatch_ReturnsFalse()
    {
        var database = new Mock<IMongoDatabase>();
        var vehicleCollection = CreateCollection<Vehicle>(database, MongoCollections.Vehicles);
        CreateCollection<MaintenanceItem>(database, MongoCollections.MaintenanceItems);
        CreateCollection<ServiceHistoryRecord>(database, MongoCollections.ServiceHistoryRecords);
        CreateCollection<InsurancePolicy>(database, MongoCollections.InsurancePolicies);
        var replacementFilter = default(FilterDefinition<Vehicle>);
        vehicleCollection.Collection
            .Setup(item => item.ReplaceOneAsync(
                It.IsAny<FilterDefinition<Vehicle>>(),
                It.IsAny<Vehicle>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<Vehicle>, Vehicle, ReplaceOptions, CancellationToken>((filter, _, _, _) => replacementFilter = filter)
            .ReturnsAsync(CreateReplaceResult(0));
        var repository = new VehicleRepository(
            database.Object,
            new MongoIndexInitializer(database.Object));
        var userId = Guid.NewGuid();
        var vehicle = Vehicle.Create(userId, "Garage", "Make", "Model", 2020, "PLATE");

        var updated = await repository.ReplaceAsync(userId, vehicle, expectedVersion: 9);

        Assert.False(updated);
        Assert.NotNull(replacementFilter);
        var rendered = replacementFilter!.Render(new RenderArgs<Vehicle>(
            BsonSerializer.LookupSerializer<Vehicle>(),
            BsonSerializer.SerializerRegistry,
            new PathRenderArgs(null, false),
            false,
            false,
            false,
            default));
        Assert.Equal(userId, rendered["UserId"].AsGuid);
        Assert.Equal(vehicle.Id, rendered["_id"].AsGuid);
        Assert.Equal(9, rendered[nameof(Vehicle.Version)].AsInt64);
    }

    private static ReplaceOneResult CreateReplaceResult(long matchedCount)
    {
        var acknowledgedType = typeof(ReplaceOneResult).GetNestedType(
            "Acknowledged",
            BindingFlags.Public | BindingFlags.NonPublic)!;
        return (ReplaceOneResult)Activator.CreateInstance(
            acknowledgedType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [matchedCount, (long?)0, null],
            null)!;
    }

    private static (Mock<IMongoCollection<TEntity>> Collection, Mock<IMongoIndexManager<TEntity>> Indexes) CreateCollection<TEntity>(
        Mock<IMongoDatabase> database,
        string collectionName)
    {
        var collection = new Mock<IMongoCollection<TEntity>>();
        var indexes = new Mock<IMongoIndexManager<TEntity>>();
        collection.SetupGet(item => item.Indexes).Returns(indexes.Object);
        indexes
            .Setup(item => item.CreateOneAsync(
                It.IsAny<CreateIndexModel<TEntity>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("created");
        indexes
            .Setup(item => item.CreateManyAsync(
                It.IsAny<IEnumerable<CreateIndexModel<TEntity>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["created"]);
        database
            .Setup(item => item.GetCollection<TEntity>(collectionName, It.IsAny<MongoCollectionSettings>()))
            .Returns(collection.Object);
        return (collection, indexes);
    }
}
