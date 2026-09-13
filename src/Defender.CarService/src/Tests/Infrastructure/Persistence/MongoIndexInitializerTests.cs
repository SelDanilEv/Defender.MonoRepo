using Defender.CarService.Infrastructure.Persistence;
using Defender.CarService.Domain.Entities;
using Moq;
using MongoDB.Driver;

namespace Defender.CarService.Tests.Infrastructure.Persistence;

public sealed class MongoIndexInitializerTests
{
    [Fact]
    public void Definitions_ContainExactlyTheApprovedOwnershipIndexes()
    {
        var definitions = MongoIndexInitializer.GetDefinitions();

        Assert.Equal(5, definitions.Count);
        Assert.Equal(
            ["UserId:1", "Archived:1", "UpdatedAtUtc:-1"],
            definitions.Single(item => item.CollectionName == MongoCollections.Vehicles).Keys);
        Assert.Equal(
            ["UserId:1", "VehicleId:1", "Name:1"],
            definitions.Single(item => item.CollectionName == MongoCollections.MaintenanceItems).Keys);

        var historyDefinitions = definitions
            .Where(item => item.CollectionName == MongoCollections.ServiceHistoryRecords)
            .Select(item => item.Keys)
            .ToArray();
        Assert.Equal(2, historyDefinitions.Length);
        Assert.Contains(["UserId:1", "VehicleId:1", "Date:-1", "OdometerKm:-1"], historyDefinitions);
        Assert.Contains(["UserId:1", "VehicleId:1", "LinkedMaintenanceItemIds:1"], historyDefinitions);

        Assert.Equal(
            ["UserId:1", "VehicleId:1", "EndDate:-1"],
            definitions.Single(item => item.CollectionName == MongoCollections.InsurancePolicies).Keys);
    }

    [Fact]
    public async Task InitializeAsync_WhenCalledMoreThanOnce_CreatesIndexesOnce()
    {
        var database = new Mock<IMongoDatabase>();
        var vehicleCollection = CreateCollection<Vehicle>(database, MongoCollections.Vehicles);
        var maintenanceCollection = CreateCollection<MaintenanceItem>(database, MongoCollections.MaintenanceItems);
        var historyCollection = CreateCollection<ServiceHistoryRecord>(database, MongoCollections.ServiceHistoryRecords);
        var insuranceCollection = CreateCollection<InsurancePolicy>(database, MongoCollections.InsurancePolicies);

        var initializer = new MongoIndexInitializer(database.Object);
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        vehicleCollection.Indexes.Verify(item => item.CreateOneAsync(
            It.IsAny<CreateIndexModel<Vehicle>>(),
            It.IsAny<CreateOneIndexOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
        maintenanceCollection.Indexes.Verify(item => item.CreateOneAsync(
            It.IsAny<CreateIndexModel<MaintenanceItem>>(),
            It.IsAny<CreateOneIndexOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
        historyCollection.Indexes.Verify(item => item.CreateManyAsync(
            It.IsAny<IEnumerable<CreateIndexModel<ServiceHistoryRecord>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        insuranceCollection.Indexes.Verify(item => item.CreateOneAsync(
            It.IsAny<CreateIndexModel<InsurancePolicy>>(),
            It.IsAny<CreateOneIndexOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenFirstAttemptFails_RetriesOnNextCall()
    {
        var database = new Mock<IMongoDatabase>();
        var vehicleCollection = CreateCollection<Vehicle>(database, MongoCollections.Vehicles);
        CreateCollection<MaintenanceItem>(database, MongoCollections.MaintenanceItems);
        CreateCollection<ServiceHistoryRecord>(database, MongoCollections.ServiceHistoryRecords);
        CreateCollection<InsurancePolicy>(database, MongoCollections.InsurancePolicies);
        var attempts = 0;
        vehicleCollection.Indexes
            .Setup(item => item.CreateOneAsync(
                It.IsAny<CreateIndexModel<Vehicle>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => Interlocked.Increment(ref attempts) == 1
                ? Task.FromException<string>(new InvalidOperationException("temporary"))
                : Task.FromResult("created"));
        var initializer = new MongoIndexInitializer(database.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.InitializeAsync());
        await initializer.InitializeAsync();

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task InitializeAsync_WhenFirstAttemptIsCanceled_RetriesOnNextCall()
    {
        var database = new Mock<IMongoDatabase>();
        var vehicleCollection = CreateCollection<Vehicle>(database, MongoCollections.Vehicles);
        CreateCollection<MaintenanceItem>(database, MongoCollections.MaintenanceItems);
        CreateCollection<ServiceHistoryRecord>(database, MongoCollections.ServiceHistoryRecords);
        CreateCollection<InsurancePolicy>(database, MongoCollections.InsurancePolicies);
        var attempts = 0;
        vehicleCollection.Indexes
            .Setup(item => item.CreateOneAsync(
                It.IsAny<CreateIndexModel<Vehicle>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => Interlocked.Increment(ref attempts) == 1
                ? Task.FromCanceled<string>(new CancellationToken(true))
                : Task.FromResult("created"));
        var initializer = new MongoIndexInitializer(database.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => initializer.InitializeAsync());
        await initializer.InitializeAsync();

        Assert.Equal(2, attempts);
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
