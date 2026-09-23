using Defender.Portal.Application.DTOs.MyGarage;
using Defender.Portal.Application.Models.ApiRequests.MyGarage;

namespace Defender.Portal.Infrastructure.Clients.CarService;

public interface ICarServiceClient
{
    Task<VehiclePageDto> GetVehiclesAsync(bool includeArchived = false, int page = 0, int pageSize = 25, CancellationToken cancellationToken = default);

    Task<VehicleDto> CreateVehicleAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);

    Task<VehicleDetailDto> GetVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    Task<VehicleDto> UpdateVehicleAsync(Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken = default);

    Task<VehicleDto> ArchiveVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    Task<VehicleDto> UnarchiveVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceItemDto>> GetMaintenanceItemsAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    Task<MaintenanceItemDto> CreateMaintenanceItemAsync(Guid vehicleId, CreateMaintenanceItemRequest request, CancellationToken cancellationToken = default);

    Task<MaintenanceItemDto> UpdateMaintenanceItemAsync(Guid vehicleId, Guid maintenanceId, UpdateMaintenanceItemRequest request, CancellationToken cancellationToken = default);

    Task DeleteMaintenanceItemAsync(Guid vehicleId, Guid maintenanceId, CancellationToken cancellationToken = default);

    Task<ServiceHistoryPageDto> GetHistoryAsync(Guid vehicleId, int page = 0, int pageSize = 25, CancellationToken cancellationToken = default);

    Task<ServiceHistoryRecordDto> CreateHistoryAsync(Guid vehicleId, CreateServiceHistoryRequest request, CancellationToken cancellationToken = default);

    Task<ServiceHistoryRecordDto> UpdateHistoryAsync(Guid vehicleId, Guid historyId, UpdateServiceHistoryRequest request, CancellationToken cancellationToken = default);

    Task DeleteHistoryAsync(Guid vehicleId, Guid historyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InsurancePolicyDto>> GetInsurancePoliciesAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    Task<InsurancePolicyDto> CreateInsurancePolicyAsync(Guid vehicleId, CreateInsurancePolicyRequest request, CancellationToken cancellationToken = default);

    Task<InsurancePolicyDto> UpdateInsurancePolicyAsync(Guid vehicleId, Guid insuranceId, UpdateInsurancePolicyRequest request, CancellationToken cancellationToken = default);

    Task DeleteInsurancePolicyAsync(Guid vehicleId, Guid insuranceId, CancellationToken cancellationToken = default);
}
