using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Maintenance;

public sealed record UpdateMaintenanceItemCommand : IRequest<MaintenanceItemDto>
{
    public Guid VehicleId { get; init; }

    public Guid MaintenanceItemId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int? IntervalMonths { get; init; }

    public int? IntervalThousandKm { get; init; }

    public DateOnly? LastDate { get; init; }

    public long? LastOdometerKm { get; init; }
}

public sealed class UpdateMaintenanceItemCommandValidator : AbstractValidator<UpdateMaintenanceItemCommand>
{
    public UpdateMaintenanceItemCommandValidator()
    {
        RuleFor(request => request.MaintenanceItemId)
            .NotEmpty()
            .WithMessage("CAR_MAINTENANCE_NOT_FOUND");
        MaintenanceRequestValidation.AddRules(this, request => request.VehicleId, request => request.Name, request => request.IntervalMonths, request => request.IntervalThousandKm, request => request.LastDate, request => request.LastOdometerKm);
    }
}

internal static class MaintenanceRequestValidation
{
    public static void AddRules<T>(
        AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, Guid>> vehicleId,
        System.Linq.Expressions.Expression<Func<T, string>> name,
        System.Linq.Expressions.Expression<Func<T, int?>> intervalMonths,
        System.Linq.Expressions.Expression<Func<T, int?>> intervalThousandKm,
        System.Linq.Expressions.Expression<Func<T, DateOnly?>> baselineDate,
        System.Linq.Expressions.Expression<Func<T, long?>> baselineOdometer)
    {
        validator.RuleFor(vehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
        validator.RuleFor(name)
            .NotEmpty()
            .WithMessage("CAR_MAINTENANCE_NAME_REQUIRED")
            .MaximumLength(200)
            .WithMessage("CAR_MAINTENANCE_NAME_REQUIRED");
        validator.RuleFor(intervalMonths)
            .GreaterThan(0)
            .When(context => intervalMonths.Compile()(context) is not null)
            .WithMessage("CAR_MAINTENANCE_INTERVAL_INVALID");
        validator.RuleFor(intervalThousandKm)
            .GreaterThan(0)
            .When(context => intervalThousandKm.Compile()(context) is not null)
            .WithMessage("CAR_MAINTENANCE_INTERVAL_INVALID");
        validator.RuleFor(context => context)
            .Must(context => intervalMonths.Compile()(context) is not null || intervalThousandKm.Compile()(context) is not null)
            .OverridePropertyName(nameof(CreateMaintenanceItemCommand.IntervalMonths))
            .WithMessage("CAR_MAINTENANCE_INTERVAL_REQUIRED");
        validator.RuleFor(baselineOdometer)
            .GreaterThanOrEqualTo(0)
            .When(context => baselineOdometer.Compile()(context) is not null)
            .WithMessage("CAR_MAINTENANCE_BASELINE_INVALID");
        validator.RuleFor(baselineDate)
            .Must(value => !value.HasValue || value.Value != DateOnly.MinValue)
            .WithMessage("CAR_MAINTENANCE_BASELINE_INVALID");
    }
}
