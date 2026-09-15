using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Maintenance;

public sealed record DeleteMaintenanceItemCommand : IRequest<Unit>
{
    public Guid VehicleId { get; init; }

    public Guid MaintenanceItemId { get; init; }
}

public sealed class DeleteMaintenanceItemCommandValidator : AbstractValidator<DeleteMaintenanceItemCommand>
{
    public DeleteMaintenanceItemCommandValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
        RuleFor(request => request.MaintenanceItemId)
            .NotEmpty()
            .WithMessage("CAR_MAINTENANCE_NOT_FOUND");
    }
}
