using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Insurance;

public sealed record DeleteInsurancePolicyCommand : IRequest<Unit>
{
    public Guid VehicleId { get; init; }

    public Guid InsuranceId { get; init; }
}

public sealed class DeleteInsurancePolicyCommandValidator : AbstractValidator<DeleteInsurancePolicyCommand>
{
    public DeleteInsurancePolicyCommandValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
        RuleFor(request => request.InsuranceId)
            .NotEmpty()
            .WithMessage("CAR_INSURANCE_NOT_FOUND");
    }
}
