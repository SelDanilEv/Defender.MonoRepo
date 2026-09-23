using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.CarService.Domain.Entities;
using Defender.CarService.Infrastructure.Persistence;
using Defender.Common.Configuration.Options;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Defender.CarService.Infrastructure.Repositories;

public sealed class VehicleRepository : MongoRepositoryBase<Vehicle>, IVehicleRepository
{
    public VehicleRepository(IMongoDatabase database, MongoIndexInitializer indexInitializer)
        : base(database, MongoCollections.Vehicles, indexInitializer)
    {
    }

    public VehicleRepository(IOptions<MongoDbOptions> options)
        : base(options, MongoCollections.Vehicles)
    {
    }

    public Task<Vehicle?> GetByIdAsync(
        Guid userId,
        Guid vehicleId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
        => FindFirstAsync(
            CreateUserEntityFilter(userId, vehicleId),
            transactionContext,
            cancellationToken);

    public Task<IReadOnlyList<Vehicle>> GetForUserAsync(
        Guid userId,
        bool includeArchived,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        var filter = CreateUserFilter(userId);
        if (!includeArchived)
        {
            filter &= Builders<Vehicle>.Filter.Eq(nameof(Vehicle.Archived), false);
        }

        return FindManyAsync(
            filter,
            Builders<Vehicle>.Sort.Combine(
                Builders<Vehicle>.Sort.Descending(nameof(Vehicle.UpdatedAtUtc)),
                Builders<Vehicle>.Sort.Descending("_id")),
            transactionContext,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Vehicle> Items, int TotalItemsCount)> GetPageForUserAsync(
        Guid userId,
        bool includeArchived,
        int page,
        int pageSize,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        var filter = CreateUserFilter(userId);
        if (!includeArchived)
        {
            filter &= Builders<Vehicle>.Filter.Eq(nameof(Vehicle.Archived), false);
        }

        var sort = Builders<Vehicle>.Sort.Combine(
            Builders<Vehicle>.Sort.Descending(nameof(Vehicle.UpdatedAtUtc)),
            Builders<Vehicle>.Sort.Descending("_id"));

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
                return (Array.Empty<Vehicle>(), totalItemsCount);
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

    public async Task<Vehicle> AddAsync(
        Guid userId,
        Vehicle vehicle,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        EnsureUser(userId, vehicle.UserId);
        await InsertAsync(vehicle, transactionContext, cancellationToken);
        return vehicle;
    }

    public async Task<bool> ReplaceAsync(
        Guid userId,
        Vehicle vehicle,
        long expectedVersion,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        EnsureUser(userId, vehicle.UserId);
        var result = await ReplaceAsync(
            CreateUserEntityVersionFilter(userId, vehicle.Id, expectedVersion),
            vehicle,
            transactionContext,
            cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid vehicleId,
        long expectedVersion,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        var result = await DeleteAsync(
            CreateUserEntityVersionFilter(userId, vehicleId, expectedVersion),
            transactionContext,
            cancellationToken);
        return result.DeletedCount == 1;
    }
}
