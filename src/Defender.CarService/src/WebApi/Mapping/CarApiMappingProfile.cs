using AutoMapper;
using Defender.CarService.Application.Requests.History;
using Defender.CarService.Application.Requests.Insurance;
using Defender.CarService.Application.Requests.Maintenance;
using Defender.CarService.Application.Requests.Vehicles;
using Defender.CarService.WebApi.Contracts;

namespace Defender.CarService.WebApi.Mapping;

public sealed class CarApiMappingProfile : Profile
{
    public CarApiMappingProfile()
    {
        CreateMap<CreateVehicleRequest, CreateVehicleCommand>();
        CreateMap<UpdateVehicleRequest, UpdateVehicleCommand>();
        CreateMap<CreateMaintenanceItemRequest, CreateMaintenanceItemCommand>();
        CreateMap<UpdateMaintenanceItemRequest, UpdateMaintenanceItemCommand>();
        CreateMap<CreateServiceHistoryRequest, CreateHistoryCommand>();
        CreateMap<UpdateServiceHistoryRequest, UpdateHistoryCommand>();
        CreateMap<CreateInsurancePolicyRequest, CreateInsurancePolicyCommand>();
        CreateMap<UpdateInsurancePolicyRequest, UpdateInsurancePolicyCommand>();
    }
}
