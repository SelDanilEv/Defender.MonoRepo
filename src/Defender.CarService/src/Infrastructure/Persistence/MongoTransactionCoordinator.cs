using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
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
        IClientSessionHandle session;
        try
        {
            if (indexInitializer is not null)
            {
                await indexInitializer.InitializeAsync(cancellationToken);
            }

            session = await client.StartSessionAsync(new ClientSessionOptions(), cancellationToken);
            try
            {
                session.StartTransaction();
            }
            catch (OperationCanceledException)
            {
                try
                {
                    session.Dispose();
                }
                catch
                {
                }

                throw;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw ToDatabaseException(exception);
        }

        using (session)
        {
            T result;
            try
            {
                result = await operation(new MongoTransactionContext(session));
            }
            catch (OperationCanceledException)
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
            catch
            {
                try
                {
                    await session.AbortTransactionAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw ToDatabaseException(exception);
                }

                throw;
            }

            try
            {
                await session.CommitTransactionAsync(cancellationToken);
            }
            catch (OperationCanceledException)
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
            catch (Exception exception)
            {
                try
                {
                    await session.AbortTransactionAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception abortException)
                {
                    throw ToDatabaseException(abortException);
                }

                throw ToDatabaseException(exception);
            }

            return result;
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

    private static ServiceException ToDatabaseException(Exception exception)
        => exception as ServiceException ?? new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
}
