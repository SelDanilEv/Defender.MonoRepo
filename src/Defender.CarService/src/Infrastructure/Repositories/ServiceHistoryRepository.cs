using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.CarService.Domain.Entities;
using Defender.CarService.Infrastructure.Persistence;
using Defender.Common.Configuration.Options;
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
