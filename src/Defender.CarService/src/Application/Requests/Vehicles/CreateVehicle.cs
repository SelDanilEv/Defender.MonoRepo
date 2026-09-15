using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Vehicles;

public sealed record CreateVehicleCommand : IRequest<VehicleDto>
{
    public string DisplayName { get; init; } = string.Empty;

    public string Make { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public int Year { get; init; }

    public string Plate { get; init; } = string.Empty;

    public string? Vin { get; init; }
}

public sealed class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        VehicleRequestValidation.AddIdentityRules(
            this,
            request => request.DisplayName,
            request => request.Make,
            request => request.Model,
            request => request.Year,
            request => request.Plate,
            request => request.Vin);
    }
}
