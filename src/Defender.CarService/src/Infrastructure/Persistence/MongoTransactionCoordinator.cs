using Defender.CarService.Application.Common.Interfaces.Repositories;
using MongoDB.Driver;

namespace Defender.CarService.Infrastructure.Persistence;

public sealed class MongoTransactionContext : ICarTransactionContext
{
    public MongoTransactionContext(IClientSessionHandle session)
    {
        Session = session;
    }

    public IClientSessionHandle Session { get; }
}

public sealed class MongoTransactionCoordinator : ICarTransactionCoordinator
{
    private readonly IMongoClient client;
    private readonly MongoIndexInitializer? indexInitializer;

    public MongoTransactionCoordinator(IMongoClient client, MongoIndexInitializer? indexInitializer = null)
    {
        this.client = client;
        this.indexInitializer = indexInitializer;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<ICarTransactionContext, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (indexInitializer is not null)
        {
            await indexInitializer.InitializeAsync(cancellationToken);
        }

        using var session = await client.StartSessionAsync(new ClientSessionOptions(), cancellationToken);
        session.StartTransaction();

        try
        {
            var result = await operation(new MongoTransactionContext(session));
            await session.CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch
        {
            try
            {
                await session.AbortTransactionAsync(cancellationToken);
            }
            catch
            {
            }

            throw;
        }
    }

    public Task ExecuteAsync(
        Func<ICarTransactionContext, Task> operation,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(
            async context =>
            {
                await operation(context);
                return true;
            },
            cancellationToken);
}
