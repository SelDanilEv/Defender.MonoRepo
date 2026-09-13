using Defender.CarService.Domain.Exceptions;

namespace Defender.CarService.Domain.Entities;

public sealed class Vehicle
{
    private Vehicle()
    {
    }

    private Vehicle(
        Guid userId,
        string displayName,
        string make,
        string model,
        int year,
        string plate,
        string? vin,
        TimeProvider timeProvider,
        Guid id)
    {
        Id = id;
        UserId = userId;
        SetIdentity(displayName, make, model, year, plate, vin, timeProvider, false);
        CreatedAtUtc = DomainClock.UtcNow(timeProvider);
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string Make { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public int Year { get; private set; }

    public string Plate { get; private set; } = string.Empty;

    public string? Vin { get; private set; }

    public bool Archived { get; private set; }

    public long? CurrentOdometerKm { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public bool IsArchived => Archived;

    public static Vehicle Create(
        Guid userId,
        string displayName,
        string make,
        string model,
        int year,
        string plate,
        string? vin = null,
        TimeProvider? timeProvider = null,
        Guid? id = null)
        => new(userId, displayName, make, model, year, plate, vin, timeProvider ?? TimeProvider.System, id ?? Guid.NewGuid());

    public void Update(
        string displayName,
        string make,
        string model,
        int year,
        string plate,
        string? vin,
        TimeProvider? timeProvider = null)
    {
        SetIdentity(displayName, make, model, year, plate, vin, timeProvider ?? TimeProvider.System, true);
    }

    public void UpdateIdentity(
        string displayName,
        string make,
        string model,
        int year,
        string plate,
        string? vin,
        TimeProvider? timeProvider = null)
        => Update(displayName, make, model, year, plate, vin, timeProvider);

    public void Archive(TimeProvider? timeProvider = null)
    {
        if (!Archived)
        {
            Archived = true;
            Touch(timeProvider ?? TimeProvider.System);
        }
    }

    public void Unarchive(TimeProvider? timeProvider = null)
    {
        if (Archived)
        {
            Archived = false;
            Touch(timeProvider ?? TimeProvider.System);
        }
    }

    public void SetCurrentOdometer(long? odometerKm, TimeProvider? timeProvider = null)
    {
        if (odometerKm < 0)
        {
            throw new CarDomainException(CarDomainErrorCodes.HistoryOdometerInvalid, "Current odometer cannot be negative.");
        }

        CurrentOdometerKm = odometerKm;
        Touch(timeProvider ?? TimeProvider.System);
    }

    public void EnsureActive()
    {
        if (Archived)
        {
            throw new CarDomainException(CarDomainErrorCodes.VehicleArchived, "Archived vehicle cannot be changed.");
        }
    }

    public void Touch(TimeProvider timeProvider)
    {
        Version++;
        UpdatedAtUtc = DomainClock.UtcNow(timeProvider);
    }

    private void SetIdentity(
        string displayName,
        string make,
        string model,
        int year,
        string plate,
        string? vin,
        TimeProvider timeProvider,
        bool touch)
    {
        var nextDisplayName = DomainValidation.RequiredText(displayName, 100, CarDomainErrorCodes.VehicleDisplayNameRequired, CarDomainErrorCodes.VehicleFieldTooLong);
        var nextMake = DomainValidation.RequiredText(make, 100, CarDomainErrorCodes.VehicleDisplayNameRequired, CarDomainErrorCodes.VehicleFieldTooLong);
        var nextModel = DomainValidation.RequiredText(model, 100, CarDomainErrorCodes.VehicleDisplayNameRequired, CarDomainErrorCodes.VehicleFieldTooLong);
        var nextPlate = DomainValidation.RequiredText(plate, 32, CarDomainErrorCodes.VehicleDisplayNameRequired, CarDomainErrorCodes.VehicleFieldTooLong);
        var nextYear = DomainValidation.VehicleYear(year, timeProvider);
        var nextVin = DomainValidation.Vin(vin);

        DisplayName = nextDisplayName;
        Make = nextMake;
        Model = nextModel;
        Plate = nextPlate;
        Year = nextYear;
        Vin = nextVin;

        if (touch)
        {
            Touch(timeProvider);
        }
    }
}

internal static class DomainClock
{
    public static DateTimeOffset UtcNow(TimeProvider timeProvider) => timeProvider.GetUtcNow();

    public static DateOnly Today(TimeProvider timeProvider) => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}

internal static class DomainValidation
{
    public static string RequiredText(string? value, int maxLength, string requiredCode, string tooLongCode)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length == 0)
        {
            throw new CarDomainException(requiredCode, "Value is required.");
        }

        if (result.Length > maxLength)
        {
            throw new CarDomainException(tooLongCode, $"Value cannot exceed {maxLength} characters.");
        }

        return result;
    }

    public static string? OptionalText(string? value, int maxLength, string tooLongCode)
    {
        var result = value?.Trim();
        if (result is not null && result.Length > maxLength)
        {
            throw new CarDomainException(tooLongCode, $"Value cannot exceed {maxLength} characters.");
        }

        return result;
    }

    public static int VehicleYear(int year, TimeProvider timeProvider)
    {
        var maximum = DomainClock.Today(timeProvider).Year + 1;
        if (year is < 1886 || year > maximum)
        {
            throw new CarDomainException(CarDomainErrorCodes.VehicleYearInvalid, "Vehicle year is outside the supported range.");
        }

        return year;
    }

    public static string? Vin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var result = value.Trim().ToUpperInvariant();
        if (result.Length != 17 || result.Any(character => !IsVinCharacter(character)))
        {
            throw new CarDomainException(CarDomainErrorCodes.VehicleVinInvalid, "VIN must contain 17 valid characters.");
        }

        return result;
    }

    private static bool IsVinCharacter(char character)
        => character is >= 'A' and <= 'H' or >= 'J' and <= 'N' or 'P' or 'R' or >= 'S' and <= 'Z' or >= '0' and <= '9';
}
