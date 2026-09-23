using Defender.CarService.Domain.Entities;

namespace Defender.CarService.Application.Common.Interfaces.Repositories;

public interface IInsurancePolicyRepository
{
    Task<InsurancePolicy?> GetByIdAsync(
        Guid userId,
        Guid vehicleId,
        Guid insurancePolicyId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InsurancePolicy>> GetForVehicleAsync(
        Guid userId,
        Guid vehicleId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InsurancePolicy>> GetForVehiclesAsync(
        Guid userId,
        IReadOnlyList<Guid> vehicleIds,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<InsurancePolicy> AddAsync(
        Guid userId,
        Guid vehicleId,
        InsurancePolicy insurancePolicy,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<bool> ReplaceAsync(
        Guid userId,
        Guid vehicleId,
        InsurancePolicy insurancePolicy,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid userId,
        Guid vehicleId,
        Guid insurancePolicyId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default);
}
