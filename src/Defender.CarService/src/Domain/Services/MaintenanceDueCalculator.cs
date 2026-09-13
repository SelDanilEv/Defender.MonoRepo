using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Enums;

namespace Defender.CarService.Domain.Services;

public sealed class MaintenanceDueCalculator
{
    private readonly TimeProvider _timeProvider;

    public MaintenanceDueCalculator(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public MaintenanceStatus CalculateStatus(MaintenanceItem item, long? currentOdometerKm)
        => CalculateStatus(item, currentOdometerKm, DomainDate.Today(_timeProvider));

    public MaintenanceStatus CalculateStatusForVehicle(MaintenanceItem item, Vehicle vehicle)
        => CalculateStatus(item, vehicle.CurrentOdometerKm, DomainDate.Today(_timeProvider));

    public MaintenanceStatus CalculateStatus(MaintenanceItem item, long? currentOdometerKm, DateOnly evaluationDate)
    {
        var nextDate = CalculateNextDate(item);
        var nextOdometerKm = CalculateNextOdometerKm(item);
        var odometerDimensionKnown = nextOdometerKm is not null && currentOdometerKm is not null;
        var dateOverdue = nextDate is not null && nextDate <= evaluationDate;
        var odometerOverdue = nextOdometerKm is { } nextOdometer && currentOdometerKm is { } currentOdometer && nextOdometer <= currentOdometer;

        if (dateOverdue || odometerOverdue)
        {
            return MaintenanceStatus.Overdue;
        }

        var dateDueSoon = nextDate is not null && nextDate <= evaluationDate.AddDays(30);
        var odometerDueSoon = nextOdometerKm is { } dueOdometer
            && currentOdometerKm is { } currentOdometerValue
            && dueOdometer - currentOdometerValue <= 1_000;

        if (dateDueSoon || odometerDueSoon)
        {
            return MaintenanceStatus.DueSoon;
        }

        return nextDate is null && !odometerDimensionKnown
            ? MaintenanceStatus.NotStarted
            : MaintenanceStatus.Upcoming;
    }

    public MaintenanceStatus Calculate(MaintenanceItem item, long? currentOdometerKm)
        => CalculateStatus(item, currentOdometerKm);

    public MaintenanceStatus CalculateStatus(MaintenanceItem item, DateOnly evaluationDate, long? currentOdometerKm)
        => CalculateStatus(item, currentOdometerKm, evaluationDate);

    public DateOnly? CalculateNextDate(MaintenanceItem item)
        => item.LastDate is not null && item.IntervalMonths is not null
            ? item.LastDate.Value.AddMonths(item.IntervalMonths.Value)
            : null;

    public long? CalculateNextOdometerKm(MaintenanceItem item)
        => item.LastOdometerKm is not null && item.IntervalThousandKm is not null
            ? checked(item.LastOdometerKm.Value + (long)item.IntervalThousandKm.Value * 1_000)
            : null;

    public static DateOnly? CalculateNextDate(DateOnly? lastDate, int? intervalMonths)
        => lastDate is not null && intervalMonths is not null ? lastDate.Value.AddMonths(intervalMonths.Value) : null;

    public static long? CalculateNextOdometerKm(long? lastOdometerKm, int? intervalThousandKm)
        => lastOdometerKm is not null && intervalThousandKm is not null
            ? checked(lastOdometerKm.Value + (long)intervalThousandKm.Value * 1_000)
            : null;

    public static DateOnly? NextDate(MaintenanceItem item) => new MaintenanceDueCalculator().CalculateNextDate(item);

    public static long? NextOdometerKm(MaintenanceItem item) => new MaintenanceDueCalculator().CalculateNextOdometerKm(item);

    public static MaintenanceStatus GetStatus(MaintenanceItem item, long? currentOdometerKm, DateOnly evaluationDate)
        => new MaintenanceDueCalculator().CalculateStatus(item, currentOdometerKm, evaluationDate);

    public static InsuranceStatus? CalculateInsuranceStatus(IEnumerable<InsurancePolicy> policies, DateOnly evaluationDate)
    {
        var statuses = policies.Select(policy => policy.GetStatus(evaluationDate)).ToArray();
        if (statuses.Length == 0)
        {
            return null;
        }

        if (statuses.Contains(InsuranceStatus.Active))
        {
            return InsuranceStatus.Active;
        }

        return statuses.Contains(InsuranceStatus.ExpiringSoon)
            ? InsuranceStatus.ExpiringSoon
            : InsuranceStatus.Expired;
    }
}

internal static class DomainDate
{
    public static DateOnly Today(TimeProvider timeProvider) => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
