using Defender.CarService.Domain.Exceptions;
using Defender.CarService.Domain.ValueObjects;

namespace Defender.CarService.Domain.Entities;

public sealed class MaintenanceItem
{
    private MaintenanceItem()
    {
        ManualBaselineDate = null;
        ManualBaselineOdometerKm = null;
    }

    private MaintenanceItem(
        Guid userId,
        Guid vehicleId,
        string name,
        int? intervalMonths,
        int? intervalThousandKm,
        DateOnly? manualBaselineDate,
        long? manualBaselineOdometerKm,
        TimeProvider timeProvider,
        Guid id)
    {
        Id = id;
        UserId = userId;
        VehicleId = vehicleId;
        Name = ValidateName(name);
        (IntervalMonths, IntervalThousandKm) = ValidateIntervals(intervalMonths, intervalThousandKm);
        SetManualBaselineValues(manualBaselineDate, manualBaselineOdometerKm);
        LastDate = ManualBaselineDate;
        LastOdometerKm = ManualBaselineOdometerKm;
        CreatedAtUtc = DomainClock.UtcNow(timeProvider);
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid VehicleId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int? IntervalMonths { get; private set; }

    public int? IntervalThousandKm { get; private set; }

    public DateOnly? ManualBaselineDate { get; private set; }

    public long? ManualBaselineOdometerKm { get; private set; }

    public DateOnly? LastDate { get; private set; }

    public long? LastOdometerKm { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ManualBaseline ManualBaseline => new(ManualBaselineDate, ManualBaselineOdometerKm);

    public EffectiveBaseline EffectiveBaseline => new(LastDate, LastOdometerKm);

    public static MaintenanceItem Create(
        Guid userId,
        Guid vehicleId,
        string name,
        int? intervalMonths,
        int? intervalThousandKm,
        DateOnly? manualBaselineDate = null,
        long? manualBaselineOdometerKm = null,
        TimeProvider? timeProvider = null,
        Guid? id = null)
        => new(userId, vehicleId, name, intervalMonths, intervalThousandKm, manualBaselineDate, manualBaselineOdometerKm, timeProvider ?? TimeProvider.System, id ?? Guid.NewGuid());

    public static MaintenanceItem Create(
        Vehicle vehicle,
        string name,
        int? intervalMonths,
        int? intervalThousandKm,
        DateOnly? manualBaselineDate = null,
        long? manualBaselineOdometerKm = null,
        TimeProvider? timeProvider = null,
        Guid? id = null)
    {
        vehicle.EnsureActive();
        return Create(vehicle.UserId, vehicle.Id, name, intervalMonths, intervalThousandKm, manualBaselineDate, manualBaselineOdometerKm, timeProvider, id);
    }

    public void Update(
        string name,
        int? intervalMonths,
        int? intervalThousandKm,
        bool vehicleArchived,
        TimeProvider? timeProvider = null)
    {
        EnsureVehicleActive(vehicleArchived);
        UpdateValues(name, intervalMonths, intervalThousandKm, timeProvider ?? TimeProvider.System);
    }

    public void Update(
        Vehicle vehicle,
        string name,
        int? intervalMonths,
        int? intervalThousandKm,
        TimeProvider? timeProvider = null)
    {
        EnsureVehicle(vehicle);
        UpdateValues(name, intervalMonths, intervalThousandKm, timeProvider ?? TimeProvider.System);
    }

    public void SetManualBaseline(
        Vehicle vehicle,
        DateOnly? date,
        long? odometerKm,
        bool linkedHistoryExists,
        TimeProvider? timeProvider = null)
    {
        EnsureVehicle(vehicle);
        SetManualBaselineValuesAndTouch(date, odometerKm, linkedHistoryExists, timeProvider ?? TimeProvider.System);
    }

    public void SetManualBaseline(
        DateOnly? date,
        long? odometerKm,
        bool linkedHistoryExists,
        TimeProvider? timeProvider,
        bool vehicleArchived)
    {
        EnsureVehicleActive(vehicleArchived);
        SetManualBaselineValuesAndTouch(date, odometerKm, linkedHistoryExists, timeProvider ?? TimeProvider.System);
    }

    public void ApplyEffectiveBaseline(EffectiveBaseline baseline, TimeProvider? timeProvider, bool vehicleArchived)
    {
        EnsureVehicleActive(vehicleArchived);
        ApplyEffectiveBaselineValues(baseline, timeProvider ?? TimeProvider.System);
    }

    public void ApplyEffectiveBaseline(Vehicle vehicle, EffectiveBaseline baseline, TimeProvider? timeProvider = null)
    {
        EnsureVehicle(vehicle);
        ApplyEffectiveBaselineValues(baseline, timeProvider ?? TimeProvider.System);
    }

    public void RecalculateEffectiveBaseline(IEnumerable<ServiceHistoryRecord> linkedRecords, TimeProvider? timeProvider, bool vehicleArchived)
    {
        EnsureVehicleActive(vehicleArchived);
        ApplyEffectiveBaselineValues(EffectiveBaseline.FromHistory(linkedRecords, ManualBaseline), timeProvider ?? TimeProvider.System);
    }

    public void RecalculateEffectiveBaseline(Vehicle vehicle, IEnumerable<ServiceHistoryRecord> linkedRecords, TimeProvider? timeProvider = null)
    {
        EnsureVehicle(vehicle);
        ApplyEffectiveBaselineValues(EffectiveBaseline.FromHistory(linkedRecords, ManualBaseline), timeProvider ?? TimeProvider.System);
    }

    public EffectiveBaseline CalculateEffectiveBaseline(IEnumerable<ServiceHistoryRecord> linkedRecords)
        => EffectiveBaseline.FromHistory(linkedRecords, ManualBaseline);

    public void EnsureVehicleActive(bool vehicleArchived)
    {
        if (vehicleArchived)
        {
            throw new CarDomainException(CarDomainErrorCodes.VehicleArchived, "Archived vehicle cannot be changed.");
        }
    }

    private void UpdateValues(string name, int? intervalMonths, int? intervalThousandKm, TimeProvider timeProvider)
    {
        var nextName = ValidateName(name);
        var (nextIntervalMonths, nextIntervalThousandKm) = ValidateIntervals(intervalMonths, intervalThousandKm);

        Name = nextName;
        IntervalMonths = nextIntervalMonths;
        IntervalThousandKm = nextIntervalThousandKm;
        Touch(timeProvider ?? TimeProvider.System);
    }

    private void SetManualBaselineValuesAndTouch(
        DateOnly? date,
        long? odometerKm,
        bool linkedHistoryExists,
        TimeProvider timeProvider)
    {
        if (linkedHistoryExists && (date != ManualBaselineDate || odometerKm != ManualBaselineOdometerKm))
        {
            throw new CarDomainException(CarDomainErrorCodes.MaintenanceBaselineLocked, "Manual baseline cannot change while history is linked.");
        }

        SetManualBaselineValues(date, odometerKm);
        if (!linkedHistoryExists)
        {
            LastDate = date;
            LastOdometerKm = odometerKm;
        }

        Touch(timeProvider);
    }

    private void ApplyEffectiveBaselineValues(EffectiveBaseline baseline, TimeProvider timeProvider)
    {
        LastDate = baseline.Date;
        LastOdometerKm = baseline.OdometerKm;
        Touch(timeProvider);
    }

    private void EnsureVehicle(Vehicle vehicle)
    {
        if (vehicle.Id != VehicleId || vehicle.UserId != UserId)
        {
            throw new CarDomainException(CarDomainErrorCodes.VehicleNotFound, "Vehicle does not own maintenance item.");
        }

        vehicle.EnsureActive();
    }

    private void SetManualBaselineValues(DateOnly? date, long? odometerKm)
    {
        if (odometerKm < 0)
        {
            throw new CarDomainException(CarDomainErrorCodes.MaintenanceBaselineInvalid, "Manual baseline odometer cannot be negative.");
        }

        ManualBaselineDate = date;
        ManualBaselineOdometerKm = odometerKm;
    }

    private static string ValidateName(string name)
        => DomainValidation.RequiredText(name, 200, CarDomainErrorCodes.MaintenanceNameRequired, CarDomainErrorCodes.MaintenanceNameRequired);

    private static (int? Months, int? ThousandKm) ValidateIntervals(int? intervalMonths, int? intervalThousandKm)
    {
        if (intervalMonths is null && intervalThousandKm is null)
        {
            throw new CarDomainException(CarDomainErrorCodes.MaintenanceIntervalRequired, "At least one maintenance interval is required.");
        }

        if (intervalMonths <= 0 || intervalThousandKm <= 0)
        {
            throw new CarDomainException(CarDomainErrorCodes.MaintenanceIntervalInvalid, "Maintenance intervals must be positive.");
        }

        return (intervalMonths, intervalThousandKm);
    }

    private void Touch(TimeProvider timeProvider)
    {
        UpdatedAtUtc = DomainClock.UtcNow(timeProvider);
        if (CreatedAtUtc == default)
        {
            CreatedAtUtc = UpdatedAtUtc;
        }
    }
}
