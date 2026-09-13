using AutoMapper;
using Defender.CarService.Application.DTOs;
using Defender.CarService.Domain.Entities;

namespace Defender.CarService.Application.Mappings;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Vehicle, VehicleDto>();
        CreateMap<MaintenanceItem, MaintenanceItemDto>();
        CreateMap<ServiceHistoryRecord, ServiceHistoryRecordDto>();
        CreateMap<InsurancePolicy, InsurancePolicyDto>();
    }
}
