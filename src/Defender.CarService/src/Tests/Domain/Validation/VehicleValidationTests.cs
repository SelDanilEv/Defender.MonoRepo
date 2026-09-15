using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Enums;
using Defender.CarService.Domain.Exceptions;

namespace Defender.CarService.Tests.Domain.Validation;

public sealed class VehicleValidationTests
{
    private static readonly TimeProvider Clock = new TestTimeProvider(new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_TrimsIdentityAndNormalizesVin()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "  Daily  ", "  BMW ", "  320d ", 2026, "  ABC-123 ", "wbaaaaaaaaaaaaaaa", Clock);

        Assert.Equal("Daily", vehicle.DisplayName);
        Assert.Equal("BMW", vehicle.Make);
        Assert.Equal("320d", vehicle.Model);
        Assert.Equal("ABC-123", vehicle.Plate);
        Assert.Equal("WBAAAAAAAAAAAAAAA", vehicle.Vin);
    }

    [Theory]
    [InlineData("", "CAR_VEHICLE_DISPLAY_NAME_REQUIRED")]
    [InlineData(" ", "CAR_VEHICLE_DISPLAY_NAME_REQUIRED")]
    public void Create_RejectsMissingDisplayName(string displayName, string code)
    {
        var exception = Assert.Throws<CarDomainException>(() => Vehicle.Create(Guid.NewGuid(), displayName, "BMW", "320d", 2026, "ABC", null, Clock));

        Assert.Equal(code, exception.Code);
    }

    [Fact]
    public void Create_RejectsOverlongVehicleText()
    {
        var exception = Assert.Throws<CarDomainException>(() => Vehicle.Create(Guid.NewGuid(), new string('x', 101), "BMW", "320d", 2026, "ABC", null, Clock));

        Assert.Equal("CAR_VEHICLE_FIELD_TOO_LONG", exception.Code);
    }

    [Theory]
    [InlineData(1885)]
    [InlineData(2028)]
    public void Create_RejectsYearOutsideUtcWindow(int year)
    {
        var exception = Assert.Throws<CarDomainException>(() => Vehicle.Create(Guid.NewGuid(), "Daily", "BMW", "320d", year, "ABC", null, Clock));

        Assert.Equal("CAR_VEHICLE_YEAR_INVALID", exception.Code);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("WBAAAAAAAAAAAAIAA")]
    [InlineData("WBAAAAAAAAAAAAQAA")]
    [InlineData("WBAAAAAAAAAAAA@AA")]
    public void Create_RejectsInvalidVin(string vin)
    {
        var exception = Assert.Throws<CarDomainException>(() => Vehicle.Create(Guid.NewGuid(), "Daily", "BMW", "320d", 2026, "ABC", vin, Clock));

        Assert.Equal("CAR_VEHICLE_VIN_INVALID", exception.Code);
    }

    [Fact]
    public void Archive_RejectsChildMutations()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "Daily", "BMW", "320d", 2026, "ABC", null, Clock);
        vehicle.Archive();

        var exception = Assert.Throws<CarDomainException>(() => MaintenanceItem.Create(vehicle, "Oil", 12, null, null, null, Clock));

        Assert.Equal("CAR_VEHICLE_ARCHIVED", exception.Code);
    }

    [Fact]
    public void Archive_RejectsHistoryAndInsuranceMutations()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "Daily", "BMW", "320d", 2026, "ABC", null, Clock);
        vehicle.Archive();

        var historyException = Assert.Throws<CarDomainException>(() => ServiceHistoryRecord.Create(vehicle, new DateOnly(2026, 9, 13), 100, HistoryType.Repair, "Repair", timeProvider: Clock));
        var insuranceException = Assert.Throws<CarDomainException>(() => InsurancePolicy.Create(vehicle, "Insurer", null, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), timeProvider: Clock));

        Assert.Equal("CAR_VEHICLE_ARCHIVED", historyException.Code);
        Assert.Equal("CAR_VEHICLE_ARCHIVED", insuranceException.Code);
    }

    [Fact]
    public void CurrentOdometer_CanBeRecomputedToNull()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "Daily", "BMW", "320d", 2026, "ABC", null, Clock);

        vehicle.SetCurrentOdometer(120_000, Clock);
        vehicle.SetCurrentOdometer(null, Clock);

        Assert.Null(vehicle.CurrentOdometerKm);
    }

    [Fact]
    public void Update_InvalidYearLeavesPriorStateUnchanged()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), "Daily", "BMW", "320d", 2026, "ABC", "WBAAAAAAAAAAAAAAA", Clock);

        Assert.Throws<CarDomainException>(() => vehicle.Update("Changed", "Audi", "A4", 1885, "XYZ", "WBBBBBBBBBBBBBBBB", Clock));

        Assert.Equal("Daily", vehicle.DisplayName);
        Assert.Equal("BMW", vehicle.Make);
        Assert.Equal("320d", vehicle.Model);
        Assert.Equal(2026, vehicle.Year);
        Assert.Equal("ABC", vehicle.Plate);
        Assert.Equal("WBAAAAAAAAAAAAAAA", vehicle.Vin);
    }
}
