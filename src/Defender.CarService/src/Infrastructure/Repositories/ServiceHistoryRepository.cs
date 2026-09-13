using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.CarService.Domain.Entities;
using Defender.CarService.Infrastructure.Persistence;
using Defender.Common.Configuration.Options;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Defender.CarService.Infrastructure.Repositories;

public sealed class ServiceHistoryRepository : MongoRepositoryBase<ServiceHistoryRecord>, IServiceHistoryRepository
{
    public ServiceHistoryRepository(IMongoDatabase database, MongoIndexInitializer indexInitializer)
        : base(database, MongoCollections.ServiceHistoryRecords, indexInitializer)
    {
    }

    public ServiceHistoryRepository(IOptions<MongoDbOptions> options)
        : base(options, MongoCollections.ServiceHistoryRecords)
    {
    }

    public Task<ServiceHistoryRecord?> GetByIdAsync(
        Guid userId,
        Guid vehicleId,
        Guid historyId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
        => FindFirstAsync(
            CreateUserVehicleEntityFilter(userId, vehicleId, historyId),
            transactionContext,
            cancellationToken);

    public Task<IReadOnlyList<ServiceHistoryRecord>> GetForVehicleAsync(
        Guid userId,
        Guid vehicleId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
        => FindManyAsync(
            CreateUserVehicleFilter(userId, vehicleId),
            Builders<ServiceHistoryRecord>.Sort.Combine(
                Builders<ServiceHistoryRecord>.Sort.Descending(nameof(ServiceHistoryRecord.Date)),
                Builders<ServiceHistoryRecord>.Sort.Descending(nameof(ServiceHistoryRecord.OdometerKm))),
            transactionContext,
            cancellationToken);

    public async Task<(IReadOnlyList<ServiceHistoryRecord> Items, int TotalItemsCount)> GetPageForVehicleAsync(
        Guid userId,
        Guid vehicleId,
        int page,
        int pageSize,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        var filter = CreateUserVehicleFilter(userId, vehicleId);
        var sort = Builders<ServiceHistoryRecord>.Sort.Combine(
            Builders<ServiceHistoryRecord>.Sort.Descending(nameof(ServiceHistoryRecord.Date)),
            Builders<ServiceHistoryRecord>.Sort.Descending(nameof(ServiceHistoryRecord.OdometerKm)));

        try
        {
            await EnsureIndexesAsync(cancellationToken);
            var session = GetSession(transactionContext);
            var total = session is null
                ? await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                : await Collection.CountDocumentsAsync(session, filter, cancellationToken: cancellationToken);
            var totalItemsCount = total > int.MaxValue ? int.MaxValue : (int)total;
            var offset = (long)page * pageSize;
            if (offset >= total || offset > int.MaxValue)
            {
                return (Array.Empty<ServiceHistoryRecord>(), totalItemsCount);
            }

            var query = session is null
                ? Collection.Find(filter)
                : Collection.Find(session, filter);
            var items = await query
                .Sort(sort)
                .Skip((int)offset)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);
            return (items, totalItemsCount);
        }
        catch (Exception exception) when (exception is not ServiceException)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    public Task<IReadOnlyList<ServiceHistoryRecord>> GetLinkedToMaintenanceAsync(
        Guid userId,
        Guid vehicleId,
        Guid maintenanceItemId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
        => FindManyAsync(
            CreateUserVehicleFilter(userId, vehicleId)
                & CreateGuidArrayContainsFilter(
                    nameof(ServiceHistoryRecord.LinkedMaintenanceItemIds),
                    maintenanceItemId),
            Builders<ServiceHistoryRecord>.Sort.Combine(
                Builders<ServiceHistoryRecord>.Sort.Descending(nameof(ServiceHistoryRecord.Date)),
                Builders<ServiceHistoryRecord>.Sort.Descending(nameof(ServiceHistoryRecord.OdometerKm))),
            transactionContext,
            cancellationToken);

    public async Task<ServiceHistoryRecord> AddAsync(
        Guid userId,
        Guid vehicleId,
        ServiceHistoryRecord history,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        EnsureUser(userId, history.UserId);
        EnsureVehicle(vehicleId, history.VehicleId);
        await InsertAsync(history, transactionContext, cancellationToken);
        return history;
    }

    public async Task<bool> ReplaceAsync(
        Guid userId,
        Guid vehicleId,
        ServiceHistoryRecord history,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        EnsureUser(userId, history.UserId);
        EnsureVehicle(vehicleId, history.VehicleId);
        var result = await ReplaceAsync(
            CreateUserVehicleEntityFilter(userId, vehicleId, history.Id),
            history,
            transactionContext,
            cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid vehicleId,
        Guid historyId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        var result = await DeleteAsync(
            CreateUserVehicleEntityFilter(userId, vehicleId, historyId),
            transactionContext,
            cancellationToken);
        return result.DeletedCount == 1;
    }
}
