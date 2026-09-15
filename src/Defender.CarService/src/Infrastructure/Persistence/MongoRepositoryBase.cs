using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.CarService.Domain.Entities;
using Defender.Common.Configuration.Options;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Defender.CarService.Infrastructure.Persistence;

public abstract class MongoRepositoryBase<TEntity>
{
    protected readonly IMongoDatabase Database;
    protected readonly IMongoCollection<TEntity> Collection;
    private readonly MongoIndexInitializer indexInitializer;

    protected MongoRepositoryBase(
        IMongoDatabase database,
        string collectionName,
        MongoIndexInitializer? indexInitializer = null)
    {
        MongoMappings.Register();
        Database = database;
        Collection = database.GetCollection<TEntity>(collectionName);
        this.indexInitializer = indexInitializer ?? new MongoIndexInitializer(database);
    }

    protected MongoRepositoryBase(
        IOptions<MongoDbOptions> options,
        string collectionName)
        : this(
            new MongoClient(options.Value.ConnectionString).GetDatabase(options.Value.GetDatabaseName()),
            collectionName)
    {
    }

    public static FilterDefinition<TEntity> CreateUserFilter(Guid userId)
        => CreateGuidFilter("UserId", userId);

    public static FilterDefinition<TEntity> CreateUserEntityFilter(Guid userId, Guid entityId)
        => CreateUserFilter(userId) & CreateGuidFilter("_id", entityId);

    public static FilterDefinition<TEntity> CreateUserVehicleFilter(Guid userId, Guid vehicleId)
        => CreateUserFilter(userId) & CreateGuidFilter("VehicleId", vehicleId);

    public static FilterDefinition<TEntity> CreateUserVehicleEntityFilter(Guid userId, Guid vehicleId, Guid entityId)
        => CreateUserVehicleFilter(userId, vehicleId) & CreateGuidFilter("_id", entityId);

    public static FilterDefinition<TEntity> CreateUserEntityVersionFilter(Guid userId, Guid entityId, long expectedVersion)
        => CreateUserEntityFilter(userId, entityId) & Builders<TEntity>.Filter.Eq("Version", expectedVersion);

    public static FilterDefinition<TEntity> CreateGuidArrayContainsFilter(string fieldName, Guid value)
        => new BsonDocumentFilterDefinition<TEntity>(
            new BsonDocument(
                fieldName,
                new BsonDocument("$in", new BsonArray { new BsonBinaryData(value, GuidRepresentation.Standard) })));

    private static FilterDefinition<TEntity> CreateGuidFilter(string fieldName, Guid value)
        => new BsonDocumentFilterDefinition<TEntity>(
            new BsonDocument(fieldName, new BsonBinaryData(value, GuidRepresentation.Standard)));

    protected static IClientSessionHandle? GetSession(ICarTransactionContext? transactionContext)
    {
        if (transactionContext is null)
        {
            return null;
        }

        if (transactionContext is MongoTransactionContext mongoContext)
        {
            return mongoContext.Session;
        }

        throw new ArgumentException("Transaction context was created by an unsupported persistence provider.", nameof(transactionContext));
    }

    protected async Task EnsureIndexesAsync(CancellationToken cancellationToken)
        => await indexInitializer.InitializeAsync(cancellationToken);

    protected async Task<TEntity?> FindFirstAsync(
        FilterDefinition<TEntity> filter,
        ICarTransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureIndexesAsync(cancellationToken);
            var session = GetSession(transactionContext);
            return session is null
                ? await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken)
                : await Collection.Find(session, filter).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not ServiceException)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    protected async Task<IReadOnlyList<TEntity>> FindManyAsync(
        FilterDefinition<TEntity> filter,
        SortDefinition<TEntity>? sort,
        ICarTransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureIndexesAsync(cancellationToken);
            var session = GetSession(transactionContext);
            var query = session is null
                ? Collection.Find(filter)
                : Collection.Find(session, filter);
            if (sort is not null)
            {
                query = query.Sort(sort);
            }

            return await query.ToListAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not ServiceException)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    protected async Task InsertAsync(
        TEntity entity,
        ICarTransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureIndexesAsync(cancellationToken);
            var session = GetSession(transactionContext);
            if (session is null)
            {
                await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
            }
            else
            {
                await Collection.InsertOneAsync(session, entity, cancellationToken: cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not ServiceException)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    protected async Task<ReplaceOneResult> ReplaceAsync(
        FilterDefinition<TEntity> filter,
        TEntity entity,
        ICarTransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureIndexesAsync(cancellationToken);
            var options = new ReplaceOptions { IsUpsert = false };
            var session = GetSession(transactionContext);
            return session is null
                ? await Collection.ReplaceOneAsync(filter, entity, options, cancellationToken)
                : await Collection.ReplaceOneAsync(session, filter, entity, options, cancellationToken);
        }
        catch (Exception exception) when (exception is not ServiceException)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    protected async Task<DeleteResult> DeleteAsync(
        FilterDefinition<TEntity> filter,
        ICarTransactionContext? transactionContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureIndexesAsync(cancellationToken);
            var session = GetSession(transactionContext);
            return session is null
                ? await Collection.DeleteOneAsync(filter, cancellationToken)
                : await Collection.DeleteOneAsync(session, filter, cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is not ServiceException)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    protected static void EnsureUser(Guid requestedUserId, Guid entityUserId)
    {
        if (requestedUserId != entityUserId)
        {
            throw new InvalidOperationException("Entity ownership does not match requested user.");
        }
    }

    protected static void EnsureVehicle(Guid requestedVehicleId, Guid entityVehicleId)
    {
        if (requestedVehicleId != entityVehicleId)
        {
            throw new InvalidOperationException("Entity vehicle does not match requested vehicle.");
        }
    }
}
