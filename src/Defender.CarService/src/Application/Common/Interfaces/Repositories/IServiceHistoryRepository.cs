using Defender.CarService.Domain.Entities;

namespace Defender.CarService.Application.Common.Interfaces.Repositories;

public interface IServiceHistoryRepository
{
    Task<ServiceHistoryRecord?> GetByIdAsync(
        Guid userId,
        Guid vehicleId,
        Guid historyId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceHistoryRecord>> GetForVehicleAsync(
        Guid userId,
        Guid vehicleId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceHistoryRecord>> GetLinkedToMaintenanceAsync(
        Guid userId,
        Guid vehicleId,
        Guid maintenanceItemId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<ServiceHistoryRecord> AddAsync(
        Guid userId,
        Guid vehicleId,
        ServiceHistoryRecord history,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<bool> ReplaceAsync(
        Guid userId,
        Guid vehicleId,
        ServiceHistoryRecord history,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid userId,
        Guid vehicleId,
        Guid historyId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);
}
