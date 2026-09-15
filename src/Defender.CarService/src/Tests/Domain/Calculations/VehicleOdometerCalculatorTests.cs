using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Enums;
using Defender.CarService.Domain.Exceptions;
using Defender.CarService.Domain.Services;
using Defender.CarService.Domain.ValueObjects;

namespace Defender.CarService.Tests.Domain.Calculations;

public sealed class VehicleOdometerCalculatorTests
{
    private static readonly TimeProvider Clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void CurrentOdometer_ReturnsHighestRemainingHistoryValueOrNull()
    {
        var records = new[]
        {
            History(new DateOnly(2026, 1, 1), 120),
            History(new DateOnly(2026, 2, 1), 100),
        };

        Assert.Equal(120, VehicleOdometerCalculator.CalculateCurrentOdometer(records));
        Assert.Null(VehicleOdometerCalculator.CalculateCurrentOdometer([]));
    }

    [Fact]
    public void Chronology_AllowsBackdatedLowerOdometerAndSameDateDifferences()
    {
        var records = new[]
        {
            History(new DateOnly(2026, 1, 1), 100),
            History(new DateOnly(2026, 1, 1), 120),
            History(new DateOnly(2026, 2, 1), 130),
        };

        VehicleOdometerCalculator.ValidateChronologicalSequence(records);
    }

    [Fact]
    public void Chronology_RejectsOlderDateWithHigherOdometer()
    {
        var records = new[]
        {
            History(new DateOnly(2026, 1, 1), 130),
            History(new DateOnly(2026, 2, 1), 120),
        };

        var exception = Assert.Throws<CarDomainException>(() => VehicleOdometerCalculator.ValidateChronologicalSequence(records));

        Assert.Equal("CAR_ODOMETER_SEQUENCE_INVALID", exception.Code);
    }

    [Fact]
    public void EffectiveBaseline_UsesLatestDateHighestOdometerThenLowestRecordId()
    {
        var maintenanceId = Guid.NewGuid();
        var lowId = new Guid("00000000-0000-0000-0000-000000000001");
        var highId = new Guid("00000000-0000-0000-0000-000000000002");
        var records = new[]
        {
            History(new DateOnly(2026, 1, 1), 200, highId, maintenanceId),
            History(new DateOnly(2026, 2, 1), 100, highId, maintenanceId),
            History(new DateOnly(2026, 2, 1), 200, highId, maintenanceId),
            History(new DateOnly(2026, 2, 1), 200, lowId, maintenanceId),
        };

        var baseline = EffectiveBaseline.FromHistory(records, new ManualBaseline(new DateOnly(2025, 1, 1), 10));

        Assert.Equal(new DateOnly(2026, 2, 1), baseline.Date);
        Assert.Equal(200, baseline.OdometerKm);
        Assert.Equal(lowId, baseline.RecordId);
    }

    [Fact]
    public void EffectiveBaseline_FallsBackToManualWhenNoLinkedHistory()
    {
        var manual = new ManualBaseline(new DateOnly(2025, 1, 1), 10);

        var baseline = EffectiveBaseline.FromHistory([], manual);

        Assert.Equal(manual.Date, baseline.Date);
        Assert.Equal(manual.OdometerKm, baseline.OdometerKm);
        Assert.Null(baseline.RecordId);
    }

    private static ServiceHistoryRecord History(DateOnly date, long odometerKm, Guid? id = null, Guid? maintenanceId = null)
        => ServiceHistoryRecord.Create(Guid.NewGuid(), Guid.NewGuid(), date, odometerKm, HistoryType.Maintenance, "Service", null, maintenanceId == null ? null : [maintenanceId.Value], null, null, Clock, id);
}
