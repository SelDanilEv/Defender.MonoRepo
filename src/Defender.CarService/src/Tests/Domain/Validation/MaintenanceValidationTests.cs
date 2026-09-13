using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Exceptions;

namespace Defender.CarService.Tests.Domain.Validation;

public sealed class MaintenanceValidationTests
{
    private static readonly TimeProvider Clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_RequiresAtLeastOnePositiveInterval()
    {
        var exception = Assert.Throws<CarDomainException>(() => MaintenanceItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Oil", null, null, null, null, Clock));

        Assert.Equal("CAR_MAINTENANCE_INTERVAL_REQUIRED", exception.Code);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(null, 0)]
    [InlineData(null, -1)]
    public void Create_RejectsNonPositiveIntervals(int? months, int? thousandKm)
    {
        var exception = Assert.Throws<CarDomainException>(() => MaintenanceItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Oil", months, thousandKm, null, null, Clock));

        Assert.Equal("CAR_MAINTENANCE_INTERVAL_INVALID", exception.Code);
    }

    [Fact]
    public void Create_RejectsNegativeManualOdometer()
    {
        var exception = Assert.Throws<CarDomainException>(() => MaintenanceItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Oil", 12, null, null, -1, Clock));

        Assert.Equal("CAR_MAINTENANCE_BASELINE_INVALID", exception.Code);
    }

    [Fact]
    public void Update_ManualBaselineLockedWhenHistoryLinkedButNameAndIntervalRemainEditable()
    {
        var item = MaintenanceItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Oil", 12, null, null, null, Clock);

        var exception = Assert.Throws<CarDomainException>(() => item.SetManualBaseline(new DateOnly(2026, 1, 1), 10_000, true, Clock));
        item.Update("  Brake oil  ", 6, null, false, Clock);

        Assert.Equal("CAR_MAINTENANCE_BASELINE_LOCKED", exception.Code);
        Assert.Equal("Brake oil", item.Name);
        Assert.Equal(6, item.IntervalMonths);
    }
}
