using AutoMapper;
using Defender.CarService.Application.DTOs;
using Defender.CarService.Domain.Entities;

namespace Defender.CarService.Application.Mappings;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Vehicle, VehicleDto>();
        CreateMap<MaintenanceItem, MaintenanceItemDto>()
            .ForMember(destination => destination.NextDate, options => options.Ignore())
            .ForMember(destination => destination.NextOdometerKm, options => options.Ignore())
            .ForMember(destination => destination.Status, options => options.Ignore());
        CreateMap<ServiceHistoryRecord, ServiceHistoryRecordDto>();
        CreateMap<InsurancePolicy, InsurancePolicyDto>()
            .ForMember(destination => destination.Status, options => options.Ignore());
    }
}
