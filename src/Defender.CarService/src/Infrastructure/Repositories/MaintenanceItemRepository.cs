using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Exceptions;
using Defender.CarService.Domain.ValueObjects;
using Defender.CarService.Infrastructure.Persistence;
using Defender.Common.Configuration.Options;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Defender.CarService.Infrastructure.Repositories;

public sealed class MaintenanceItemRepository : MongoRepositoryBase<MaintenanceItem>, IMaintenanceItemRepository
{
    private readonly IMongoDatabase database;

    public MaintenanceItemRepository(IMongoDatabase database, MongoIndexInitializer indexInitializer)
        : base(database, MongoCollections.MaintenanceItems, indexInitializer)
    {
        this.database = database;
    }

    public MaintenanceItemRepository(IOptions<MongoDbOptions> options)
        : base(options, MongoCollections.MaintenanceItems)
    {
        database = Database;
    }

    public Task<MaintenanceItem?> GetByIdAsync(
        Guid userId,
        Guid vehicleId,
        Guid maintenanceItemId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
        => FindFirstAsync(
            CreateUserVehicleEntityFilter(userId, vehicleId, maintenanceItemId),
            transactionContext,
            cancellationToken);

    public Task<IReadOnlyList<MaintenanceItem>> GetForVehicleAsync(
        Guid userId,
        Guid vehicleId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
        => FindManyAsync(
            CreateUserVehicleFilter(userId, vehicleId),
            Builders<MaintenanceItem>.Sort.Ascending(nameof(MaintenanceItem.Name)),
            transactionContext,
            cancellationToken);

    public async Task<MaintenanceItem> AddAsync(
        Guid userId,
        Guid vehicleId,
        MaintenanceItem maintenanceItem,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        EnsureUser(userId, maintenanceItem.UserId);
        EnsureVehicle(vehicleId, maintenanceItem.VehicleId);
        await InsertAsync(maintenanceItem, transactionContext, cancellationToken);
        return maintenanceItem;
    }

    public async Task<bool> ReplaceAsync(
        Guid userId,
        Guid vehicleId,
        MaintenanceItem maintenanceItem,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        EnsureUser(userId, maintenanceItem.UserId);
        EnsureVehicle(vehicleId, maintenanceItem.VehicleId);
        var result = await ReplaceAsync(
            CreateUserVehicleEntityFilter(userId, vehicleId, maintenanceItem.Id),
            maintenanceItem,
            transactionContext,
            cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid vehicleId,
        Guid maintenanceItemId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureIndexesAsync(cancellationToken);
            var historyCollection = database.GetCollection<ServiceHistoryRecord>(MongoCollections.ServiceHistoryRecords);
            var referencesFilter = MongoRepositoryBase<ServiceHistoryRecord>.CreateUserVehicleFilter(userId, vehicleId)
                & MongoRepositoryBase<ServiceHistoryRecord>.CreateGuidArrayContainsFilter(
                    nameof(ServiceHistoryRecord.LinkedMaintenanceItemIds),
                    maintenanceItemId);
            var references = GetSession(transactionContext) is { } session
                ? await historyCollection.CountDocumentsAsync(session, referencesFilter, cancellationToken: cancellationToken)
                : await historyCollection.CountDocumentsAsync(referencesFilter, cancellationToken: cancellationToken);
            if (references > 0)
            {
                throw new CarDomainException(CarDomainErrorCodes.MaintenanceReferenced, "Maintenance item is referenced by service history.");
            }

            var result = await DeleteAsync(
                CreateUserVehicleEntityFilter(userId, vehicleId, maintenanceItemId),
                transactionContext,
                cancellationToken);
            return result.DeletedCount == 1;
        }
        catch (Exception exception) when (exception is not ServiceException && exception is not CarDomainException)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }

    public async Task<bool> ReplaceEffectiveBaselineAsync(
        Guid userId,
        Guid vehicleId,
        Guid maintenanceItemId,
        EffectiveBaseline baseline,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        var update = Builders<MaintenanceItem>.Update
            .Set(nameof(MaintenanceItem.LastDate), baseline.Date)
            .Set(nameof(MaintenanceItem.LastOdometerKm), baseline.OdometerKm)
            .Set(nameof(MaintenanceItem.UpdatedAtUtc), DateTimeOffset.UtcNow);
        var filter = CreateUserVehicleEntityFilter(userId, vehicleId, maintenanceItemId);
        try
        {
            await EnsureIndexesAsync(cancellationToken);
            var session = GetSession(transactionContext);
            var result = session is null
                ? await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
                : await Collection.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken);
            return result.MatchedCount == 1;
        }
        catch (Exception exception) when (exception is not ServiceException)
        {
            throw new ServiceException(ErrorCode.CM_DatabaseIssue, exception);
        }
    }
}
