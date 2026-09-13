using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Vehicles;

public sealed record UpdateVehicleCommand : IRequest<VehicleDto>
{
    public Guid VehicleId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Make { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public int Year { get; init; }

    public string Plate { get; init; } = string.Empty;

    public string? Vin { get; init; }
}

public sealed class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
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

internal static class VehicleRequestValidation
{
    public static void AddIdentityRules<T>(
        AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string>> displayName,
        System.Linq.Expressions.Expression<Func<T, string>> make,
        System.Linq.Expressions.Expression<Func<T, string>> model,
        System.Linq.Expressions.Expression<Func<T, int>> year,
        System.Linq.Expressions.Expression<Func<T, string>> plate,
        System.Linq.Expressions.Expression<Func<T, string?>> vin)
    {
        validator.RuleFor(displayName)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_DISPLAY_NAME_REQUIRED")
            .MaximumLength(100)
            .WithMessage("CAR_VEHICLE_FIELD_TOO_LONG");
        validator.RuleFor(make)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_DISPLAY_NAME_REQUIRED")
            .MaximumLength(100)
            .WithMessage("CAR_VEHICLE_FIELD_TOO_LONG");
        validator.RuleFor(model)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_DISPLAY_NAME_REQUIRED")
            .MaximumLength(100)
            .WithMessage("CAR_VEHICLE_FIELD_TOO_LONG");
        validator.RuleFor(plate)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_DISPLAY_NAME_REQUIRED")
            .MaximumLength(32)
            .WithMessage("CAR_VEHICLE_FIELD_TOO_LONG");
        validator.RuleFor(year)
            .InclusiveBetween(1886, DateTime.UtcNow.Year + 1)
            .WithMessage("CAR_VEHICLE_YEAR_INVALID");
        validator.RuleFor(vin)
            .Must(IsValidVin)
            .WithMessage("CAR_VEHICLE_VIN_INVALID");
    }

    private static bool IsValidVin(string? vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
        {
            return true;
        }

        var value = vin.Trim().ToUpperInvariant();
        return value.Length == 17
            && value.All(character => character is >= 'A' and <= 'H' or >= 'J' and <= 'N' or 'P' or 'R' or >= 'S' and <= 'Z' or >= '0' and <= '9');
    }
}
