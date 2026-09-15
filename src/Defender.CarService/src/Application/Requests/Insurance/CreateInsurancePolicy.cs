using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Insurance;

public sealed record CreateInsurancePolicyCommand : IRequest<InsurancePolicyDto>
{
    public Guid VehicleId { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string? PolicyNumber { get; init; }

    public string? CoverageType { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public string? Notes { get; init; }
}

public sealed class CreateInsurancePolicyCommandValidator : AbstractValidator<CreateInsurancePolicyCommand>
{
    public CreateInsurancePolicyCommandValidator()
    {
        InsuranceRequestValidation.AddRules(this, request => request.VehicleId, request => request.Provider, request => request.PolicyNumber, request => request.CoverageType, request => request.StartDate, request => request.EndDate, request => request.Notes);
    }
}
