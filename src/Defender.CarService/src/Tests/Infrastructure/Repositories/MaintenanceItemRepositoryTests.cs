using Defender.CarService.Domain.Entities;
using Defender.CarService.Infrastructure.Persistence;
using Defender.CarService.Infrastructure.Repositories;
using Defender.Common.Exceptions;
using Moq;
using MongoDB.Driver;

namespace Defender.CarService.Tests.Infrastructure.Repositories;

public sealed class MaintenanceItemRepositoryTests
{
    [Fact]
    public async Task DeleteAsync_WhenReferenceCountFails_WrapsDatabaseFailure()
    {
        var database = new Mock<IMongoDatabase>();
        CreateCollection<Vehicle>(database, MongoCollections.Vehicles);
        CreateCollection<MaintenanceItem>(database, MongoCollections.MaintenanceItems);
        CreateCollection<InsurancePolicy>(database, MongoCollections.InsurancePolicies);
        var historyCollection = CreateCollection<ServiceHistoryRecord>(database, MongoCollections.ServiceHistoryRecords);
        historyCollection.Collection
            .Setup(item => item.CountDocumentsAsync(
                It.IsAny<FilterDefinition<ServiceHistoryRecord>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Mongo unavailable"));
        var repository = new MaintenanceItemRepository(
            database.Object,
            new MongoIndexInitializer(database.Object));

        var exception = await Assert.ThrowsAsync<ServiceException>(() => repository.DeleteAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            cancellationToken: CancellationToken.None));

        Assert.Contains("CM_DatabaseIssue", exception.Message, StringComparison.Ordinal);
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
