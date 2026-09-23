using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.ValueObjects;

namespace Defender.CarService.Application.Common.Interfaces.Repositories;

public interface IMaintenanceItemRepository
{
    Task<MaintenanceItem?> GetByIdAsync(
        Guid userId,
        Guid vehicleId,
        Guid maintenanceItemId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceItem>> GetForVehicleAsync(
        Guid userId,
        Guid vehicleId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceItem>> GetForVehiclesAsync(
        Guid userId,
        IReadOnlyList<Guid> vehicleIds,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<MaintenanceItem> AddAsync(
        Guid userId,
        Guid vehicleId,
        MaintenanceItem maintenanceItem,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<bool> ReplaceAsync(
        Guid userId,
        Guid vehicleId,
        MaintenanceItem maintenanceItem,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid userId,
        Guid vehicleId,
        Guid maintenanceItemId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<bool> ReplaceEffectiveBaselineAsync(
        Guid userId,
        Guid vehicleId,
        Guid maintenanceItemId,
        EffectiveBaseline baseline,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);
}
