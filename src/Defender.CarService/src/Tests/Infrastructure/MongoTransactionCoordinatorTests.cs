using Defender.CarService.Infrastructure.Persistence;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using MongoDB.Driver;
using Moq;

namespace Defender.CarService.Tests.Infrastructure;

public sealed class MongoTransactionCoordinatorTests
{
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
}
