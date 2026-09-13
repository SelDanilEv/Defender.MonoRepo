using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.CarService.Domain.Entities;
using Defender.CarService.Infrastructure.Persistence;
using Defender.Common.Configuration.Options;
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
            Builders<Vehicle>.Sort.Descending(nameof(Vehicle.UpdatedAtUtc)),
            transactionContext,
            cancellationToken);
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
