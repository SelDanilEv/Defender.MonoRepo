using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Maintenance;

public sealed record GetMaintenanceItemsQuery : IRequest<IReadOnlyList<MaintenanceItemDto>>
{
    public Guid VehicleId { get; init; }
}

public sealed class GetMaintenanceItemsQueryValidator : AbstractValidator<GetMaintenanceItemsQuery>
{
    public GetMaintenanceItemsQueryValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
    }
}
