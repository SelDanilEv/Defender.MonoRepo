using Defender.CarService.Infrastructure.Persistence;
using Defender.CarService.Domain.Entities;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using MongoDB.Driver;
using Moq;

namespace Defender.CarService.Tests.Infrastructure;

public sealed class MongoTransactionCoordinatorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenIndexInitializationIsCanceled_PreservesCancellation()
    {
        var database = new Mock<IMongoDatabase>();
        var vehicleCollection = CreateCollection<Vehicle>(database, MongoCollections.Vehicles);
        CreateCollection<MaintenanceItem>(database, MongoCollections.MaintenanceItems);
        CreateCollection<ServiceHistoryRecord>(database, MongoCollections.ServiceHistoryRecords);
        CreateCollection<InsurancePolicy>(database, MongoCollections.InsurancePolicies);
        vehicleCollection.Indexes
            .Setup(value => value.CreateOneAsync(
                It.IsAny<CreateIndexModel<Vehicle>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromCanceled<string>(new CancellationToken(true)));
        var coordinator = new MongoTransactionCoordinator(
            new Mock<IMongoClient>().Object,
            new MongoIndexInitializer(database.Object));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.ExecuteAsync(
            _ => Task.FromResult(1),
            new CancellationToken(true)));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSessionStartFails_ReturnsDatabaseServiceException()
    {
        var client = new Mock<IMongoClient>();
        client.Setup(value => value.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("session unavailable"));
        var coordinator = new MongoTransactionCoordinator(client.Object);

        var exception = await Assert.ThrowsAsync<ServiceException>(() => coordinator.ExecuteAsync(
            _ => Task.FromResult(1),
            CancellationToken.None));

        Assert.True(exception.IsErrorCode(ErrorCode.CM_DatabaseIssue));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSessionStartIsCanceled_PreservesCancellation()
    {
        var client = new Mock<IMongoClient>();
        client.Setup(value => value.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("session canceled"));
        var coordinator = new MongoTransactionCoordinator(client.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.ExecuteAsync(
            _ => Task.FromResult(1),
            CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTransactionStartFails_ReturnsDatabaseServiceException()
    {
        var client = new Mock<IMongoClient>();
        var session = new Mock<IClientSessionHandle>();
        session.Setup(value => value.StartTransaction(It.IsAny<TransactionOptions>()))
            .Throws(new InvalidOperationException("transaction unavailable"));
        client.Setup(value => value.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);
        var coordinator = new MongoTransactionCoordinator(client.Object);

        var exception = await Assert.ThrowsAsync<ServiceException>(() => coordinator.ExecuteAsync(
            _ => Task.FromResult(1),
            CancellationToken.None));

        Assert.True(exception.IsErrorCode(ErrorCode.CM_DatabaseIssue));
        session.Verify(value => value.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTransactionStartIsCanceled_PreservesCancellation()
    {
        var client = new Mock<IMongoClient>();
        var session = new Mock<IClientSessionHandle>();
        session.Setup(value => value.StartTransaction(It.IsAny<TransactionOptions>()))
            .Throws(new OperationCanceledException("transaction canceled"));
        client.Setup(value => value.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);
        var coordinator = new MongoTransactionCoordinator(client.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.ExecuteAsync(
            _ => Task.FromResult(1),
            CancellationToken.None));

        session.Verify(value => value.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationIsCanceled_PreservesCancellation()
    {
        var client = new Mock<IMongoClient>();
        var session = new Mock<IClientSessionHandle>();
        session.Setup(value => value.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        client.Setup(value => value.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);
        var coordinator = new MongoTransactionCoordinator(client.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.ExecuteAsync<int>(
            _ => throw new OperationCanceledException("operation canceled"),
            CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommitIsCanceled_PreservesCancellation()
    {
        var client = new Mock<IMongoClient>();
        var session = new Mock<IClientSessionHandle>();
        session.Setup(value => value.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("commit canceled"));
        session.Setup(value => value.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        client.Setup(value => value.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);
        var coordinator = new MongoTransactionCoordinator(client.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.ExecuteAsync(
            _ => Task.FromResult(1),
            CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommitAndAbortFail_ReturnsDatabaseServiceException()
    {
        var client = new Mock<IMongoClient>();
        var session = new Mock<IClientSessionHandle>();
        session.Setup(value => value.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("commit unavailable"));
        session.Setup(value => value.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("abort unavailable"));
        client.Setup(value => value.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);
        var coordinator = new MongoTransactionCoordinator(client.Object);

        var exception = await Assert.ThrowsAsync<ServiceException>(() => coordinator.ExecuteAsync(
            _ => Task.FromResult(1),
            CancellationToken.None));

        Assert.True(exception.IsErrorCode(ErrorCode.CM_DatabaseIssue));
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationAndRollbackFail_ReturnsDatabaseServiceException()
    {
        var client = new Mock<IMongoClient>();
        var session = new Mock<IClientSessionHandle>();
        session.Setup(value => value.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("abort unavailable"));
        client.Setup(value => value.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);
        var coordinator = new MongoTransactionCoordinator(client.Object);

        var exception = await Assert.ThrowsAsync<ServiceException>(() => coordinator.ExecuteAsync<int>(
            _ => throw new InvalidOperationException("callback failed"),
            CancellationToken.None));

        Assert.True(exception.IsErrorCode(ErrorCode.CM_DatabaseIssue));
    }

    private static (Mock<IMongoCollection<TEntity>> Collection, Mock<IMongoIndexManager<TEntity>> Indexes) CreateCollection<TEntity>(
        Mock<IMongoDatabase> database,
        string collectionName)
    {
        var collection = new Mock<IMongoCollection<TEntity>>();
        var indexes = new Mock<IMongoIndexManager<TEntity>>();
        collection.SetupGet(value => value.Indexes).Returns(indexes.Object);
        indexes
            .Setup(value => value.CreateOneAsync(
                It.IsAny<CreateIndexModel<TEntity>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("created");
        indexes
            .Setup(value => value.CreateManyAsync(
                It.IsAny<IEnumerable<CreateIndexModel<TEntity>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["created"]);
        database
            .Setup(value => value.GetCollection<TEntity>(collectionName, It.IsAny<MongoCollectionSettings>()))
            .Returns(collection.Object);
        return (collection, indexes);
    }
}
