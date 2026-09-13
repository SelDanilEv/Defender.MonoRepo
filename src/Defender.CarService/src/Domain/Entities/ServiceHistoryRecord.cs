using Defender.CarService.Domain.Enums;
using Defender.CarService.Domain.Exceptions;
using Defender.CarService.Domain.ValueObjects;

namespace Defender.CarService.Domain.Entities;

public sealed class ServiceHistoryRecord
{
    private ServiceHistoryRecord()
    {
        LinkedMaintenanceItemIds = [];
        Cost = Cost.None;
    }

    private ServiceHistoryRecord(
        Guid userId,
        Guid vehicleId,
        DateOnly date,
        long odometerKm,
        HistoryType type,
        string title,
        string? notes,
        IEnumerable<Guid>? linkedMaintenanceItemIds,
        long? costAmountMinor,
        Currency? costCurrency,
        TimeProvider timeProvider,
        Guid id)
    {
        Id = id;
        UserId = userId;
        VehicleId = vehicleId;
        SetDetails(date, odometerKm, type, title, notes, linkedMaintenanceItemIds, costAmountMinor, costCurrency, timeProvider, false);
        CreatedAtUtc = DomainClock.UtcNow(timeProvider);
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid VehicleId { get; private set; }

    public DateOnly Date { get; private set; }

    public long OdometerKm { get; private set; }

    public HistoryType Type { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Notes { get; private set; }

    public long? CostAmountMinor { get; private set; }

    public Currency? CostCurrency { get; private set; }

    public IReadOnlyList<Guid> LinkedMaintenanceItemIds { get; private set; } = [];

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Cost Cost { get; private set; } = ValueObjects.Cost.None;

    public static ServiceHistoryRecord Create(
        Guid userId,
        Guid vehicleId,
        DateOnly date,
        long odometerKm,
        HistoryType type,
        string title,
        string? notes = null,
        IEnumerable<Guid>? linkedMaintenanceItemIds = null,
        long? costAmountMinor = null,
        Currency? costCurrency = null,
        TimeProvider? timeProvider = null,
        Guid? id = null)
        => new(userId, vehicleId, date, odometerKm, type, title, notes, linkedMaintenanceItemIds, costAmountMinor, costCurrency, timeProvider ?? TimeProvider.System, id ?? Guid.NewGuid());

    public static ServiceHistoryRecord Create(
        Vehicle vehicle,
        DateOnly date,
        long odometerKm,
        HistoryType type,
        string title,
        string? notes = null,
        IEnumerable<Guid>? linkedMaintenanceItemIds = null,
        long? costAmountMinor = null,
        Currency? costCurrency = null,
        TimeProvider? timeProvider = null,
        Guid? id = null)
    {
        vehicle.EnsureActive();
        return Create(vehicle.UserId, vehicle.Id, date, odometerKm, type, title, notes, linkedMaintenanceItemIds, costAmountMinor, costCurrency, timeProvider, id);
    }

    public void Update(
        DateOnly date,
        long odometerKm,
        HistoryType type,
        string title,
        string? notes = null,
        IEnumerable<Guid>? linkedMaintenanceItemIds = null,
        long? costAmountMinor = null,
        Currency? costCurrency = null,
        bool vehicleArchived = false,
        TimeProvider? timeProvider = null)
    {
        EnsureVehicleActive(vehicleArchived);
        SetDetails(date, odometerKm, type, title, notes, linkedMaintenanceItemIds, costAmountMinor, costCurrency, timeProvider ?? TimeProvider.System, true);
    }

    public void EnsureVehicleActive(bool vehicleArchived = false)
    {
        if (vehicleArchived)
        {
            throw new CarDomainException(CarDomainErrorCodes.VehicleArchived, "Archived vehicle cannot be changed.");
        }
    }

    private void SetDetails(
        DateOnly date,
        long odometerKm,
        HistoryType type,
        string title,
        string? notes,
        IEnumerable<Guid>? linkedMaintenanceItemIds,
        long? costAmountMinor,
        Currency? costCurrency,
        TimeProvider timeProvider,
        bool touch)
    {
        var today = DomainClock.Today(timeProvider);
        if (date == default)
        {
            throw new CarDomainException(CarDomainErrorCodes.HistoryDateInvalid, "History date is required.");
        }

        if (date > today)
        {
            throw new CarDomainException(CarDomainErrorCodes.HistoryDateFuture, "History date cannot be in the future.");
        }

        if (odometerKm < 0)
        {
            throw new CarDomainException(CarDomainErrorCodes.HistoryOdometerInvalid, "History odometer cannot be negative.");
        }

        if (type is HistoryType.Unknown or < HistoryType.Maintenance or > HistoryType.Other)
        {
            throw new CarDomainException(CarDomainErrorCodes.HistoryTypeInvalid, "History type is not supported.");
        }

        Title = DomainValidation.RequiredText(title, 200, CarDomainErrorCodes.HistoryTitleRequired, CarDomainErrorCodes.HistoryTitleRequired);
        Notes = DomainValidation.OptionalText(notes, 2_000, CarDomainErrorCodes.HistoryTitleRequired);
        LinkedMaintenanceItemIds = NormalizeLinks(linkedMaintenanceItemIds);
        Cost = new Cost(costAmountMinor, costCurrency);
        CostAmountMinor = Cost.AmountMinor;
        CostCurrency = Cost.Currency;
        Date = date;
        OdometerKm = odometerKm;
        Type = type;

        if (touch)
        {
            Touch(timeProvider);
        }
    }

    private static IReadOnlyList<Guid> NormalizeLinks(IEnumerable<Guid>? linkedMaintenanceItemIds)
    {
        var ids = (linkedMaintenanceItemIds ?? []).ToArray();
        if (ids.Any(id => id == Guid.Empty))
        {
            throw new CarDomainException(CarDomainErrorCodes.HistoryLinkInvalid, "Linked maintenance ID cannot be empty.");
        }

        return ids.Distinct().ToArray();
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
