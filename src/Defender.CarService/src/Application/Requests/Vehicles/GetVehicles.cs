using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Vehicles;

public sealed record GetVehiclesQuery : IRequest<IReadOnlyList<VehicleSummaryDto>>
{
    public bool IncludeArchived { get; init; }
}

public sealed class GetVehiclesQueryValidator : AbstractValidator<GetVehiclesQuery>
{
}

public sealed record GetVehicleQuery : IRequest<VehicleDetailDto>
{
    public Guid VehicleId { get; init; }
}

public sealed class GetVehicleQueryValidator : AbstractValidator<GetVehicleQuery>
{
    public GetVehicleQueryValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
    }
}
