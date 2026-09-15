using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Vehicles;

public sealed record ArchiveVehicleCommand : IRequest<VehicleDto>
{
    public Guid VehicleId { get; init; }
}

public sealed class ArchiveVehicleCommandValidator : AbstractValidator<ArchiveVehicleCommand>
{
    public ArchiveVehicleCommandValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
    }
}
