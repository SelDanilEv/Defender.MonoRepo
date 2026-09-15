using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Enums;
using Defender.CarService.Domain.Exceptions;
using Defender.CarService.Domain.ValueObjects;

namespace Defender.CarService.Tests.Domain.Validation;

public sealed class HistoryValidationTests
{
    private static readonly TimeProvider Clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_RejectsFutureDate()
    {
        var exception = Assert.Throws<CarDomainException>(() => ServiceHistoryRecord.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 14), 100, HistoryType.Maintenance, "Service", null, null, null, null, Clock));

        Assert.Equal("CAR_HISTORY_DATE_FUTURE", exception.Code);
    }

    [Fact]
    public void Create_RejectsNegativeOdometer()
    {
        var exception = Assert.Throws<CarDomainException>(() => ServiceHistoryRecord.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 13), -1, HistoryType.Maintenance, "Service", null, null, null, null, Clock));

        Assert.Equal("CAR_HISTORY_ODOMETER_INVALID", exception.Code);
    }

    [Fact]
    public void Create_DeduplicatesLinkedMaintenanceIds()
    {
        var id = Guid.NewGuid();
        var history = ServiceHistoryRecord.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 13), 100, HistoryType.Repair, "Service", null, [id, id, Guid.NewGuid()], null, null, Clock);

        Assert.Equal(2, history.LinkedMaintenanceItemIds.Count);
    }

    [Fact]
    public void Create_RejectsUnknownHistoryType()
    {
        var exception = Assert.Throws<CarDomainException>(() => ServiceHistoryRecord.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 13), 100, HistoryType.Unknown, "Service", null, null, null, null, Clock));

        Assert.Equal("CAR_HISTORY_TYPE_INVALID", exception.Code);
    }

    [Theory]
    [InlineData(100L, null)]
    [InlineData(null, Currency.EUR)]
    public void Create_RejectsOneSidedCostPair(long? amount, Currency? currency)
    {
        var exception = Assert.Throws<CarDomainException>(() => ServiceHistoryRecord.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 13), 100, HistoryType.Maintenance, "Service", null, null, amount, currency, Clock));

        Assert.Equal("CAR_HISTORY_COST_PAIR_INVALID", exception.Code);
    }

    [Fact]
    public void Create_RejectsNegativeOrUnknownCost()
    {
        var negative = Assert.Throws<CarDomainException>(() => ServiceHistoryRecord.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 13), 100, HistoryType.Maintenance, "Service", null, null, -1, Currency.EUR, Clock));
        var unknown = Assert.Throws<CarDomainException>(() => ServiceHistoryRecord.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 13), 100, HistoryType.Maintenance, "Service", null, null, 100, Currency.Unknown, Clock));

        Assert.Equal("CAR_HISTORY_COST_INVALID", negative.Code);
        Assert.Equal("CAR_HISTORY_COST_INVALID", unknown.Code);
    }

    [Fact]
    public void Create_RejectsUndefinedCurrencyValue()
    {
        var exception = Assert.Throws<CarDomainException>(() => Cost.Create(100, (Currency)999));

        Assert.Equal("CAR_CURRENCY_INVALID", exception.Code);
    }

    [Fact]
    public void Update_RejectsArchivedVehicleContext()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "Daily", "BMW", "320d", 2026, "ABC", null, Clock);
        var history = ServiceHistoryRecord.Create(vehicle, new DateOnly(2026, 9, 13), 100, HistoryType.Repair, "Repair", timeProvider: Clock);
        vehicle.Archive(Clock);

        var exception = Assert.Throws<CarDomainException>(() => history.Update(vehicle, new DateOnly(2026, 9, 13), 110, HistoryType.Repair, "Repair 2", timeProvider: Clock));

        Assert.Equal("CAR_VEHICLE_ARCHIVED", exception.Code);
    }
}
