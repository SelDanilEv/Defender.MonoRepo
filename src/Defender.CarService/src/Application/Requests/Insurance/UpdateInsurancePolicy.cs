using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Insurance;

public sealed record UpdateInsurancePolicyCommand : IRequest<InsurancePolicyDto>
{
    public Guid VehicleId { get; init; }

    public Guid InsurancePolicyId { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string? PolicyNumber { get; init; }

    public string? CoverageType { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public string? Notes { get; init; }
}

public sealed class UpdateInsurancePolicyCommandValidator : AbstractValidator<UpdateInsurancePolicyCommand>
{
    public UpdateInsurancePolicyCommandValidator()
    {
        RuleFor(request => request.InsurancePolicyId)
            .NotEmpty()
            .WithMessage("CAR_INSURANCE_NOT_FOUND");
        InsuranceRequestValidation.AddRules(this, request => request.VehicleId, request => request.Provider, request => request.PolicyNumber, request => request.CoverageType, request => request.StartDate, request => request.EndDate, request => request.Notes);
    }
}

internal static class InsuranceRequestValidation
{
    public static void AddRules<T>(
        AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, Guid>> vehicleId,
        System.Linq.Expressions.Expression<Func<T, string>> provider,
        System.Linq.Expressions.Expression<Func<T, string?>> policyNumber,
        System.Linq.Expressions.Expression<Func<T, string?>> coverageType,
        System.Linq.Expressions.Expression<Func<T, DateOnly>> startDate,
        System.Linq.Expressions.Expression<Func<T, DateOnly>> endDate,
        System.Linq.Expressions.Expression<Func<T, string?>> notes)
    {
        validator.RuleFor(vehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
        validator.RuleFor(provider)
            .NotEmpty()
            .WithMessage("CAR_INSURANCE_PROVIDER_REQUIRED")
            .MaximumLength(200)
            .WithMessage("CAR_INSURANCE_FIELD_TOO_LONG");
        validator.RuleFor(policyNumber)
            .MaximumLength(100)
            .When(value => value is not null)
            .WithMessage("CAR_INSURANCE_FIELD_TOO_LONG");
        validator.RuleFor(coverageType)
            .MaximumLength(100)
            .When(value => value is not null)
            .WithMessage("CAR_INSURANCE_FIELD_TOO_LONG");
        validator.RuleFor(notes)
            .MaximumLength(2_000)
            .When(value => value is not null)
            .WithMessage("CAR_INSURANCE_FIELD_TOO_LONG");
        validator.RuleFor(startDate)
            .Must(value => value != DateOnly.MinValue)
            .WithMessage("CAR_INSURANCE_DATE_RANGE_INVALID");
        validator.RuleFor(endDate)
            .Must(value => value != DateOnly.MinValue)
            .WithMessage("CAR_INSURANCE_DATE_RANGE_INVALID");
        validator.RuleFor(context => context)
            .Must(context => startDate.Compile()(context) <= endDate.Compile()(context))
            .WithMessage("CAR_INSURANCE_DATE_RANGE_INVALID");
    }
}
