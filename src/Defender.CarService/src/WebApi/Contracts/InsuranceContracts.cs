namespace Defender.CarService.WebApi.Contracts;

public sealed class CreateInsurancePolicyRequest
{
    public string Provider { get; init; } = string.Empty;

    public string? PolicyNumber { get; init; }

    public string? CoverageType { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public string? Notes { get; init; }
}

public sealed class UpdateInsurancePolicyRequest
{
    public string Provider { get; init; } = string.Empty;

    public string? PolicyNumber { get; init; }

    public string? CoverageType { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public string? Notes { get; init; }
}
