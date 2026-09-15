using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.CarService.Domain.Entities;
using Defender.CarService.Infrastructure.Persistence;
using Defender.Common.Configuration.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Defender.CarService.Infrastructure.Repositories;

public sealed class InsurancePolicyRepository : MongoRepositoryBase<InsurancePolicy>, IInsurancePolicyRepository
{
    public InsurancePolicyRepository(IMongoDatabase database, MongoIndexInitializer indexInitializer)
        : base(database, MongoCollections.InsurancePolicies, indexInitializer)
    {
    }

    public InsurancePolicyRepository(IOptions<MongoDbOptions> options)
        : base(options, MongoCollections.InsurancePolicies)
    {
    }

    public Task<InsurancePolicy?> GetByIdAsync(
        Guid userId,
        Guid vehicleId,
        Guid insurancePolicyId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
        => FindFirstAsync(
            CreateUserVehicleEntityFilter(userId, vehicleId, insurancePolicyId),
            transactionContext,
            cancellationToken);

    public Task<IReadOnlyList<InsurancePolicy>> GetForVehicleAsync(
        Guid userId,
        Guid vehicleId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
        => FindManyAsync(
            CreateUserVehicleFilter(userId, vehicleId),
            Builders<InsurancePolicy>.Sort.Descending(nameof(InsurancePolicy.EndDate)),
            transactionContext,
            cancellationToken);

    public async Task<InsurancePolicy> AddAsync(
        Guid userId,
        Guid vehicleId,
        InsurancePolicy insurancePolicy,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        EnsureUser(userId, insurancePolicy.UserId);
        EnsureVehicle(vehicleId, insurancePolicy.VehicleId);
        await InsertAsync(insurancePolicy, transactionContext, cancellationToken);
        return insurancePolicy;
    }

    public async Task<bool> ReplaceAsync(
        Guid userId,
        Guid vehicleId,
        InsurancePolicy insurancePolicy,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        EnsureUser(userId, insurancePolicy.UserId);
        EnsureVehicle(vehicleId, insurancePolicy.VehicleId);
        var result = await ReplaceAsync(
            CreateUserVehicleEntityFilter(userId, vehicleId, insurancePolicy.Id),
            insurancePolicy,
            transactionContext,
            cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid vehicleId,
        Guid insurancePolicyId,
        ICarTransactionContext? transactionContext = null,
        CancellationToken cancellationToken = default)
    {
        var result = await DeleteAsync(
            CreateUserVehicleEntityFilter(userId, vehicleId, insurancePolicyId),
            transactionContext,
            cancellationToken);
        return result.DeletedCount == 1;
    }
}
