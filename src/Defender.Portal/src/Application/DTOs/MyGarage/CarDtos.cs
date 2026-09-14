using Defender.Portal.Application.Enums;

namespace Defender.Portal.Application.DTOs.MyGarage;

public enum HistoryType
{
    Unknown = 0,
    Maintenance,
    Repair,
    Tire,
    Other,
}

public enum MaintenanceStatus
{
    NotStarted = 0,
    Overdue,
    DueSoon,
    Upcoming,
}

public enum InsuranceStatus
{
    Active = 0,
    ExpiringSoon,
    Expired,
}

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

public sealed class MaintenanceItemDto
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int? IntervalMonths { get; init; }

    public int? IntervalThousandKm { get; init; }

    public DateOnly? LastDate { get; init; }

    public long? LastOdometerKm { get; init; }

    public DateOnly? ManualBaselineDate { get; init; }

    public long? ManualBaselineOdometerKm { get; init; }

    public DateOnly? NextDate { get; init; }

    public long? NextOdometerKm { get; init; }

    public MaintenanceStatus Status { get; init; }

    public bool HasLinkedHistory { get; init; }
}

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

public sealed class ServiceHistoryPageDto
{
    public IReadOnlyList<ServiceHistoryRecordDto> Items { get; init; } = [];

    public int TotalItemsCount { get; init; }

    public int CurrentPage { get; init; }

    public int PageSize { get; init; }

    public int TotalPagesCount { get; init; }
}

public sealed class InsurancePolicyDto
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string? PolicyNumber { get; init; }

    public string? CoverageType { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public string? Notes { get; init; }

    public InsuranceStatus Status { get; init; }
}
