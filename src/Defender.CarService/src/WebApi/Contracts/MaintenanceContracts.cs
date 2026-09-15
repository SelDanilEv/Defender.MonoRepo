namespace Defender.CarService.WebApi.Contracts;

public sealed class CreateMaintenanceItemRequest
{
    public string Name { get; init; } = string.Empty;

    public int? IntervalMonths { get; init; }

    public int? IntervalThousandKm { get; init; }

    public DateOnly? LastDate { get; init; }

    public long? LastOdometerKm { get; init; }

    public DateOnly? ManualBaselineDate { get; init; }

    public long? ManualBaselineOdometerKm { get; init; }
}

public sealed class UpdateMaintenanceItemRequest
{
    public string Name { get; init; } = string.Empty;

    public int? IntervalMonths { get; init; }

    public int? IntervalThousandKm { get; init; }

    public DateOnly? LastDate { get; init; }

    public long? LastOdometerKm { get; init; }

    public DateOnly? ManualBaselineDate { get; init; }

    public long? ManualBaselineOdometerKm { get; init; }
}
