using Defender.CarService.Domain.Enums;

namespace Defender.CarService.Domain.Entities;

public sealed class InsurancePolicy
{
    private InsurancePolicy()
    {
    }

    private InsurancePolicy(
        Guid userId,
        Guid vehicleId,
        string provider,
        string? policyNumber,
        string? coverageType,
        DateOnly startDate,
        DateOnly endDate,
        string? notes,
        TimeProvider timeProvider,
        Guid id)
    {
        Id = id;
        UserId = userId;
        VehicleId = vehicleId;
        SetDetails(provider, policyNumber, coverageType, startDate, endDate, notes, timeProvider, false);
        CreatedAtUtc = DomainClock.UtcNow(timeProvider);
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid VehicleId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string? PolicyNumber { get; private set; }

    public string? CoverageType { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static InsurancePolicy Create(
        Guid userId,
        Guid vehicleId,
        string provider,
        string? policyNumber,
        string? coverageType,
        DateOnly startDate,
        DateOnly endDate,
        string? notes = null,
        TimeProvider? timeProvider = null,
        Guid? id = null)
        => new(userId, vehicleId, provider, policyNumber, coverageType, startDate, endDate, notes, timeProvider ?? TimeProvider.System, id ?? Guid.NewGuid());

    public static InsurancePolicy Create(
        Vehicle vehicle,
        string provider,
        string? policyNumber,
        string? coverageType,
        DateOnly startDate,
        DateOnly endDate,
        string? notes = null,
        TimeProvider? timeProvider = null,
        Guid? id = null)
    {
        vehicle.EnsureActive();
        return Create(vehicle.UserId, vehicle.Id, provider, policyNumber, coverageType, startDate, endDate, notes, timeProvider, id);
    }

    public void Update(
        string provider,
        string? policyNumber,
        string? coverageType,
        DateOnly startDate,
        DateOnly endDate,
        string? notes = null,
        bool vehicleArchived = false,
        TimeProvider? timeProvider = null)
    {
        if (vehicleArchived)
        {
            throw new Exceptions.CarDomainException(Exceptions.CarDomainErrorCodes.VehicleArchived, "Archived vehicle cannot be changed.");
        }

        SetDetails(provider, policyNumber, coverageType, startDate, endDate, notes, timeProvider ?? TimeProvider.System, true);
    }

    public InsuranceStatus GetStatus(DateOnly evaluationDate)
    {
        if (EndDate < evaluationDate)
        {
            return InsuranceStatus.Expired;
        }

        return EndDate <= evaluationDate.AddDays(30)
            ? InsuranceStatus.ExpiringSoon
            : InsuranceStatus.Active;
    }

    public InsuranceStatus CalculateStatus(DateOnly evaluationDate) => GetStatus(evaluationDate);

    public InsuranceStatus GetStatus(TimeProvider timeProvider) => GetStatus(DomainClock.Today(timeProvider));

    private void SetDetails(
        string provider,
        string? policyNumber,
        string? coverageType,
        DateOnly startDate,
        DateOnly endDate,
        string? notes,
        TimeProvider timeProvider,
        bool touch)
    {
        if (endDate < startDate)
        {
            throw new Exceptions.CarDomainException(Exceptions.CarDomainErrorCodes.InsuranceDateRangeInvalid, "Insurance end date cannot precede start date.");
        }

        Provider = DomainValidation.RequiredText(provider, 200, Exceptions.CarDomainErrorCodes.InsuranceProviderRequired, Exceptions.CarDomainErrorCodes.InsuranceFieldTooLong);
        PolicyNumber = DomainValidation.OptionalText(policyNumber, 100, Exceptions.CarDomainErrorCodes.InsuranceFieldTooLong);
        CoverageType = DomainValidation.OptionalText(coverageType, 100, Exceptions.CarDomainErrorCodes.InsuranceFieldTooLong);
        Notes = DomainValidation.OptionalText(notes, 2_000, Exceptions.CarDomainErrorCodes.InsuranceFieldTooLong);
        StartDate = startDate;
        EndDate = endDate;

        if (touch)
        {
            UpdatedAtUtc = DomainClock.UtcNow(timeProvider);
        }
    }
}
