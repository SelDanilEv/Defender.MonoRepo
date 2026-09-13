using Defender.CarService.Application.DTOs;
using Defender.CarService.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.History;

public sealed record UpdateHistoryCommand : IRequest<ServiceHistoryRecordDto>
{
    public Guid VehicleId { get; init; }

    public Guid HistoryId { get; init; }

    public DateOnly Date { get; init; }

    public long OdometerKm { get; init; }

    public HistoryType Type { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public IReadOnlyList<Guid> LinkedMaintenanceItemIds { get; init; } = [];

    public long? CostAmountMinor { get; init; }

    public Currency? CostCurrency { get; init; }
}

public sealed class UpdateHistoryCommandValidator : AbstractValidator<UpdateHistoryCommand>
{
    public UpdateHistoryCommandValidator()
    {
        RuleFor(request => request.HistoryId)
            .NotEmpty()
            .WithMessage("CAR_HISTORY_NOT_FOUND");
        HistoryRequestValidation.AddRules(this, request => request.VehicleId, request => request.Date, request => request.OdometerKm, request => request.Type, request => request.Title, request => request.Notes, request => request.LinkedMaintenanceItemIds, request => request.CostAmountMinor, request => request.CostCurrency);
    }
}

internal static class HistoryRequestValidation
{
    public static void AddRules<T>(
        AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, Guid>> vehicleId,
        System.Linq.Expressions.Expression<Func<T, DateOnly>> date,
        System.Linq.Expressions.Expression<Func<T, long>> odometerKm,
        System.Linq.Expressions.Expression<Func<T, HistoryType>> type,
        System.Linq.Expressions.Expression<Func<T, string>> title,
        System.Linq.Expressions.Expression<Func<T, string?>> notes,
        System.Linq.Expressions.Expression<Func<T, IReadOnlyList<Guid>>> links,
        System.Linq.Expressions.Expression<Func<T, long?>> costAmount,
        System.Linq.Expressions.Expression<Func<T, Currency?>> costCurrency)
    {
        validator.RuleFor(vehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
        validator.RuleFor(date)
            .Must(value => value != DateOnly.MinValue)
            .WithMessage("CAR_HISTORY_DATE_INVALID")
            .Must(value => value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("CAR_HISTORY_DATE_FUTURE");
        validator.RuleFor(odometerKm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CAR_HISTORY_ODOMETER_INVALID");
        validator.RuleFor(type)
            .Must(value => value is >= HistoryType.Maintenance and <= HistoryType.Other && Enum.IsDefined(value))
            .WithMessage("CAR_HISTORY_TYPE_INVALID");
        validator.RuleFor(title)
            .NotEmpty()
            .WithMessage("CAR_HISTORY_TITLE_REQUIRED")
            .MaximumLength(200)
            .WithMessage("CAR_HISTORY_TITLE_REQUIRED");
        validator.RuleFor(notes)
            .MaximumLength(2_000)
            .When(value => value is not null)
            .WithMessage("CAR_HISTORY_TITLE_REQUIRED");
        validator.RuleFor(links)
            .Must(values => values.All(value => value != Guid.Empty) && values.Distinct().Count() == values.Count)
            .WithMessage("CAR_HISTORY_LINK_INVALID");
        validator.RuleFor(context => context)
            .Must(context => costAmount.Compile()(context) is null == (costCurrency.Compile()(context) is null))
            .OverridePropertyName(nameof(CreateHistoryCommand.CostAmountMinor))
            .WithMessage("CAR_HISTORY_COST_PAIR_INVALID");
        validator.RuleFor(costAmount)
            .GreaterThanOrEqualTo(0)
            .When(context => costAmount.Compile()(context) is not null)
            .WithMessage("CAR_HISTORY_COST_INVALID");
        validator.RuleFor(costCurrency)
            .Must(value => value is null || (value.Value != Currency.Unknown && Enum.IsDefined(value.Value)))
            .WithMessage("CAR_HISTORY_COST_INVALID");
    }
}
