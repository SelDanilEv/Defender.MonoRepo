using Defender.CarService.Domain.Enums;

namespace Defender.CarService.Application.DTOs;

public sealed class VehicleDto
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Make { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public int Year { get; init; }

    public string Plate { get; init; } = string.Empty;

    public string? Vin { get; init; }

    public bool Archived { get; init; }

    public long? CurrentOdometerKm { get; init; }

    public long Version { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }
}

public sealed class MaintenanceStatusCountsDto
{
    public int Overdue { get; init; }

    public int DueSoon { get; init; }

    public int Upcoming { get; init; }

    public int NotStarted { get; init; }
}

public sealed class VehicleSummaryDto
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string Make { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public int Year { get; init; }

    public string Plate { get; init; } = string.Empty;

    public string? Vin { get; init; }

    public bool Archived { get; init; }

    public long? CurrentOdometerKm { get; init; }

    public MaintenanceStatusCountsDto MaintenanceCounts { get; init; } = new();

    public InsuranceStatus? InsuranceStatus { get; init; }
}

public sealed class VehicleDetailDto
{
    public VehicleDto Vehicle { get; init; } = new();

    public IReadOnlyList<MaintenanceItemDto> MaintenanceItems { get; init; } = [];

    public IReadOnlyList<InsurancePolicyDto> InsurancePolicies { get; init; } = [];
}
