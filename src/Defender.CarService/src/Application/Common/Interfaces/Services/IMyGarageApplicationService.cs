using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Requests.History;
using Defender.CarService.Application.Requests.Insurance;
using Defender.CarService.Application.Requests.Maintenance;
using Defender.CarService.Application.Requests.Vehicles;
using MediatR;

namespace Defender.CarService.Application.Common.Interfaces.Services;

public interface IMyGarageApplicationService
{
    Task<IReadOnlyList<VehicleSummaryDto>> GetVehiclesAsync(bool includeArchived, CancellationToken cancellationToken);

    Task<VehicleDetailDto> GetVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<VehicleDto> CreateVehicleAsync(CreateVehicleCommand request, CancellationToken cancellationToken);

    Task<VehicleDto> UpdateVehicleAsync(UpdateVehicleCommand request, CancellationToken cancellationToken);

    Task<VehicleDto> ArchiveVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<VehicleDto> UnarchiveVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenanceItemDto>> GetMaintenanceItemsAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<MaintenanceItemDto> CreateMaintenanceItemAsync(CreateMaintenanceItemCommand request, CancellationToken cancellationToken);

    Task<MaintenanceItemDto> UpdateMaintenanceItemAsync(UpdateMaintenanceItemCommand request, CancellationToken cancellationToken);

    Task<Unit> DeleteMaintenanceItemAsync(DeleteMaintenanceItemCommand request, CancellationToken cancellationToken);

    Task<ServiceHistoryPageDto> GetHistoryAsync(GetHistoryQuery request, CancellationToken cancellationToken);

    Task<ServiceHistoryRecordDto> CreateHistoryAsync(CreateHistoryCommand request, CancellationToken cancellationToken);

    Task<ServiceHistoryRecordDto> UpdateHistoryAsync(UpdateHistoryCommand request, CancellationToken cancellationToken);

    Task<Unit> DeleteHistoryAsync(DeleteHistoryCommand request, CancellationToken cancellationToken);

    Task<IReadOnlyList<InsurancePolicyDto>> GetInsurancePoliciesAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<InsurancePolicyDto> CreateInsurancePolicyAsync(CreateInsurancePolicyCommand request, CancellationToken cancellationToken);

    Task<InsurancePolicyDto> UpdateInsurancePolicyAsync(UpdateInsurancePolicyCommand request, CancellationToken cancellationToken);
}
