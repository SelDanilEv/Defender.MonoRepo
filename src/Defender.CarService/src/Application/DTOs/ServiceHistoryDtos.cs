using Defender.CarService.Domain.Enums;

namespace Defender.CarService.Application.DTOs;

public sealed class ServiceHistoryRecordDto
{
    public Guid Id { get; init; }

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

public sealed class ServiceHistoryPageDto : PageDto<ServiceHistoryRecordDto>
{
}
