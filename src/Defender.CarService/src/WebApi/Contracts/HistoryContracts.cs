using Defender.CarService.Domain.Enums;

namespace Defender.CarService.WebApi.Contracts;

public sealed class CreateServiceHistoryRequest
{
    public DateOnly Date { get; init; }

    public long OdometerKm { get; init; }

    public HistoryType Type { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public IReadOnlyList<Guid>? LinkedMaintenanceItemIds { get; init; } = [];

    public long? CostAmountMinor { get; init; }

    public Currency? CostCurrency { get; init; }
}

public sealed class UpdateServiceHistoryRequest
{
    public DateOnly Date { get; init; }

    public long OdometerKm { get; init; }

    public HistoryType Type { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public IReadOnlyList<Guid>? LinkedMaintenanceItemIds { get; init; } = [];

    public long? CostAmountMinor { get; init; }

    public Currency? CostCurrency { get; init; }
}
