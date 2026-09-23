using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Vehicles;

public sealed record GetVehiclesQuery : IRequest<VehiclePageDto>
{
    public bool IncludeArchived { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; } = 25;
}

public sealed class GetVehiclesQueryValidator : AbstractValidator<GetVehiclesQuery>
{
    public GetVehiclesQueryValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CAR_VEHICLES_PAGINATION_INVALID");
        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("CAR_VEHICLES_PAGINATION_INVALID");
    }
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
