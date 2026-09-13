using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Enums;
using Defender.CarService.Domain.Services;

namespace Defender.CarService.Tests.Domain.Calculations;

public sealed class MaintenanceDueCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 9, 13);
    private static readonly TimeProvider Clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void NextValues_UseSpreadsheetFormulas()
    {
        var item = MaintenanceItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Oil", 1, 2, new DateOnly(2026, 1, 31), 100_000, Clock);
        var calculator = new MaintenanceDueCalculator(Clock);

        Assert.Equal(new DateOnly(2026, 2, 28), calculator.CalculateNextDate(item));
        Assert.Equal(102_000, calculator.CalculateNextOdometerKm(item));
    }

    [Theory]
    [InlineData(0, MaintenanceStatus.Overdue)]
    [InlineData(30, MaintenanceStatus.DueSoon)]
    [InlineData(31, MaintenanceStatus.Upcoming)]
    public void DateStatus_UsesInclusiveBoundaries(int daysAfterToday, MaintenanceStatus expected)
    {
        var nextDate = Today.AddDays(daysAfterToday);
        var item = MaintenanceItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Oil", 1, null, nextDate.AddMonths(-1), null, Clock);
        var actual = new MaintenanceDueCalculator(Clock).CalculateStatus(item, (long?)null);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0, MaintenanceStatus.Overdue)]
    [InlineData(1000, MaintenanceStatus.DueSoon)]
    [InlineData(1001, MaintenanceStatus.Upcoming)]
    public void OdometerStatus_UsesInclusiveBoundaries(long remainingKm, MaintenanceStatus expected)
    {
        var item = MaintenanceItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Oil", null, 1, null, 100_000, Clock);
        var actual = new MaintenanceDueCalculator(Clock).CalculateStatus(item, 100_000 + 1_000 - remainingKm);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Status_IgnoresMissingDimensionsAndReturnsNotStartedWithoutBaseline()
    {
        var missingOdometer = MaintenanceItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Date only", 2, 1, Today, null, Clock);
        var missingVehicleOdometer = MaintenanceItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Odometer only", null, 1, null, 100_000, Clock);
        var noBaseline = MaintenanceItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Empty", 1, null, null, null, Clock);
        var calculator = new MaintenanceDueCalculator(Clock);

        Assert.Equal(MaintenanceStatus.Upcoming, calculator.CalculateStatus(missingOdometer, (long?)null));
        Assert.Equal(MaintenanceStatus.NotStarted, calculator.CalculateStatus(missingVehicleOdometer, (long?)null));
        Assert.Equal(MaintenanceStatus.NotStarted, calculator.CalculateStatus(noBaseline, 100_000));
    }

    [Fact]
    public void InsuranceStatus_UsesUtcDateAndInclusiveThirtyDays()
    {
        var policy = InsurancePolicy.Create(Guid.NewGuid(), Guid.NewGuid(), "Insurer", null, null, Today, Today.AddDays(30), null, Clock);

        Assert.Equal(InsuranceStatus.ExpiringSoon, policy.GetStatus(Today));
        Assert.Equal(InsuranceStatus.Active, policy.GetStatus(Today.AddDays(-1)));
        Assert.Equal(InsuranceStatus.Expired, policy.GetStatus(Today.AddDays(31)));
    }

    [Fact]
    public void InsuranceSummary_PrioritizesActiveThenExpiringSoon()
    {
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var soon = InsurancePolicy.Create(userId, vehicleId, "Soon", null, null, Today, Today.AddDays(30), null, Clock);
        var active = InsurancePolicy.Create(userId, vehicleId, "Active", null, null, Today, Today.AddDays(31), null, Clock);

        Assert.Equal(InsuranceStatus.Active, MaintenanceDueCalculator.CalculateInsuranceStatus([soon, active], Today));
        Assert.Equal(InsuranceStatus.ExpiringSoon, MaintenanceDueCalculator.CalculateInsuranceStatus([soon], Today));
        Assert.Equal(InsuranceStatus.Expired, MaintenanceDueCalculator.CalculateInsuranceStatus([soon], Today.AddDays(31)));
        Assert.Null(MaintenanceDueCalculator.CalculateInsuranceStatus([], Today));
    }
}
