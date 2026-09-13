using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Vehicles;

public sealed record UnarchiveVehicleCommand : IRequest<VehicleDto>
{
    public Guid VehicleId { get; init; }
}

public sealed class UnarchiveVehicleCommandValidator : AbstractValidator<UnarchiveVehicleCommand>
{
    public UnarchiveVehicleCommandValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
    }
}
