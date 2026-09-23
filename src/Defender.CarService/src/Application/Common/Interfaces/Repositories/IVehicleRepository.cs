using Defender.CarService.Domain.Entities;

namespace Defender.CarService.Application.Common.Interfaces.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(
        Guid userId,
        Guid vehicleId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> GetForUserAsync(
        Guid userId,
        bool includeArchived,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Vehicle> Items, int TotalItemsCount)> GetPageForUserAsync(
        Guid userId,
        bool includeArchived,
        int page,
        int pageSize,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<Vehicle> AddAsync(
        Guid userId,
        Vehicle vehicle,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<bool> ReplaceAsync(
        Guid userId,
        Vehicle vehicle,
        long expectedVersion,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid userId,
        Guid vehicleId,
        long expectedVersion,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);
}
