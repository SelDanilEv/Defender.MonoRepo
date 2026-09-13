using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Insurance;

public sealed record GetInsurancePoliciesQuery : IRequest<IReadOnlyList<InsurancePolicyDto>>
{
    public Guid VehicleId { get; init; }
}

public sealed class GetInsurancePoliciesQueryValidator : AbstractValidator<GetInsurancePoliciesQuery>
{
    public GetInsurancePoliciesQueryValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
    }
}
