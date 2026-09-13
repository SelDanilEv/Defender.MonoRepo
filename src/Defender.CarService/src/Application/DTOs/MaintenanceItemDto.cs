using Defender.CarService.Domain.Enums;

namespace Defender.CarService.Application.DTOs;

public sealed class MaintenanceItemDto
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int? IntervalMonths { get; init; }

    public int? IntervalThousandKm { get; init; }

    public DateOnly? LastDate { get; init; }

    public long? LastOdometerKm { get; init; }

    public DateOnly? NextDate { get; init; }

    public long? NextOdometerKm { get; init; }

    public MaintenanceStatus Status { get; init; }
}
