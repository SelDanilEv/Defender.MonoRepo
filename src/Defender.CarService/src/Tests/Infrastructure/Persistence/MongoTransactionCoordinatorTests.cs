using Defender.CarService.Infrastructure.Persistence;
using Moq;
using MongoDB.Driver;

namespace Defender.CarService.Tests.Infrastructure.Persistence;

public sealed class MongoTransactionCoordinatorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenOperationSucceeds_CommitsAndDisposesSession()
    {
        var client = new Mock<IMongoClient>();
        var session = new Mock<IClientSessionHandle>();
        client
            .Setup(item => item.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);
        var coordinator = new MongoTransactionCoordinator(client.Object);

        var result = await coordinator.ExecuteAsync(
            context =>
            {
                Assert.IsType<MongoTransactionContext>(context);
                return Task.FromResult(42);
            },
            CancellationToken.None);

        Assert.Equal(42, result);
        session.Verify(item => item.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
        session.Verify(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        session.Verify(item => item.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        session.Verify(item => item.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationFails_AbortsDisposesAndRethrows()
    {
        var client = new Mock<IMongoClient>();
        var session = new Mock<IClientSessionHandle>();
        client
            .Setup(item => item.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);
        var coordinator = new MongoTransactionCoordinator(client.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.ExecuteAsync<int>(
            _ => throw new InvalidOperationException("expected"),
            CancellationToken.None));

        session.Verify(item => item.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
        session.Verify(item => item.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        session.Verify(item => item.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        session.Verify(item => item.Dispose(), Times.Once);
    }
}
