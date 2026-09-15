using Defender.Common.Wrapper;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.MyGarage;
using Defender.Portal.Application.Models.ApiRequests.MyGarage;

namespace Defender.Portal.Infrastructure.Clients.CarService;

public sealed class CarServiceWrapper(ICarServiceClient client) : BaseSwaggerWrapper, ICarServiceWrapper
{
    public Task<IReadOnlyList<VehicleSummaryDto>> GetVehiclesAsync(bool includeArchived = false, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.GetVehiclesAsync(includeArchived, cancellationToken));

    public Task<VehicleDto> CreateVehicleAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.CreateVehicleAsync(request, cancellationToken));

    public Task<VehicleDetailDto> GetVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.GetVehicleAsync(vehicleId, cancellationToken));

    public Task<VehicleDto> UpdateVehicleAsync(Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.UpdateVehicleAsync(vehicleId, request, cancellationToken));

    public Task<VehicleDto> ArchiveVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.ArchiveVehicleAsync(vehicleId, cancellationToken));

    public Task<VehicleDto> UnarchiveVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.UnarchiveVehicleAsync(vehicleId, cancellationToken));

    public Task<IReadOnlyList<MaintenanceItemDto>> GetMaintenanceItemsAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.GetMaintenanceItemsAsync(vehicleId, cancellationToken));

    public Task<MaintenanceItemDto> CreateMaintenanceItemAsync(Guid vehicleId, CreateMaintenanceItemRequest request, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.CreateMaintenanceItemAsync(vehicleId, request, cancellationToken));

    public Task<MaintenanceItemDto> UpdateMaintenanceItemAsync(Guid vehicleId, Guid maintenanceId, UpdateMaintenanceItemRequest request, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.UpdateMaintenanceItemAsync(vehicleId, maintenanceId, request, cancellationToken));

    public Task DeleteMaintenanceItemAsync(Guid vehicleId, Guid maintenanceId, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.DeleteMaintenanceItemAsync(vehicleId, maintenanceId, cancellationToken));

    public Task<ServiceHistoryPageDto> GetHistoryAsync(Guid vehicleId, int page = 0, int pageSize = 25, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.GetHistoryAsync(vehicleId, page, pageSize, cancellationToken));

    public Task<ServiceHistoryRecordDto> CreateHistoryAsync(Guid vehicleId, CreateServiceHistoryRequest request, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.CreateHistoryAsync(vehicleId, request, cancellationToken));

    public Task<ServiceHistoryRecordDto> UpdateHistoryAsync(Guid vehicleId, Guid historyId, UpdateServiceHistoryRequest request, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.UpdateHistoryAsync(vehicleId, historyId, request, cancellationToken));

    public Task DeleteHistoryAsync(Guid vehicleId, Guid historyId, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.DeleteHistoryAsync(vehicleId, historyId, cancellationToken));

    public Task<IReadOnlyList<InsurancePolicyDto>> GetInsurancePoliciesAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.GetInsurancePoliciesAsync(vehicleId, cancellationToken));

    public Task<InsurancePolicyDto> CreateInsurancePolicyAsync(Guid vehicleId, CreateInsurancePolicyRequest request, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.CreateInsurancePolicyAsync(vehicleId, request, cancellationToken));

    public Task<InsurancePolicyDto> UpdateInsurancePolicyAsync(Guid vehicleId, Guid insuranceId, UpdateInsurancePolicyRequest request, CancellationToken cancellationToken = default) =>
        ExecuteUnsafelyAsync(() => client.UpdateInsurancePolicyAsync(vehicleId, insuranceId, request, cancellationToken));
}
