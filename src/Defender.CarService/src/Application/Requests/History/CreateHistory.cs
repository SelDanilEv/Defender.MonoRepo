using Defender.CarService.Application.DTOs;
using Defender.CarService.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.History;

public sealed record CreateHistoryCommand : IRequest<ServiceHistoryRecordDto>
{
    public Guid VehicleId { get; init; }

    public DateOnly Date { get; init; }

    public long OdometerKm { get; init; }

    public HistoryType Type { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public IReadOnlyList<Guid> LinkedMaintenanceItemIds { get; init; } = [];

    public long? CostAmountMinor { get; init; }

    public Currency? CostCurrency { get; init; }
}

public sealed class CreateHistoryCommandValidator : AbstractValidator<CreateHistoryCommand>
{
    public CreateHistoryCommandValidator()
    {
        HistoryRequestValidation.AddRules(this, request => request.VehicleId, request => request.Date, request => request.OdometerKm, request => request.Type, request => request.Title, request => request.Notes, request => request.LinkedMaintenanceItemIds, request => request.CostAmountMinor, request => request.CostCurrency);
    }
}
