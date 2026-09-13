using Defender.CarService.Domain.Enums;

namespace Defender.CarService.Application.DTOs;

public sealed class InsurancePolicyDto
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string? PolicyNumber { get; init; }

    public string? CoverageType { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public string? Notes { get; init; }

    public InsuranceStatus Status { get; init; }
}
