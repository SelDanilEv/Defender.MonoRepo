using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Common.Interfaces.Services;
using Defender.CarService.Application.Requests.Insurance;
using Defender.CarService.Application.Services;
using MediatR;

namespace Defender.CarService.Application.Handlers.Insurance;

public sealed class GetInsurancePoliciesQueryHandler(IMyGarageApplicationService service)
    : IRequestHandler<GetInsurancePoliciesQuery, IReadOnlyList<InsurancePolicyDto>>
{
    public Task<IReadOnlyList<InsurancePolicyDto>> Handle(GetInsurancePoliciesQuery request, CancellationToken cancellationToken)
        => service.GetInsurancePoliciesAsync(request.VehicleId, cancellationToken);
}

public sealed class CreateInsurancePolicyCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<CreateInsurancePolicyCommand, InsurancePolicyDto>
{
    public Task<InsurancePolicyDto> Handle(CreateInsurancePolicyCommand request, CancellationToken cancellationToken)
        => service.CreateInsurancePolicyAsync(request, cancellationToken);
}

public sealed class UpdateInsurancePolicyCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<UpdateInsurancePolicyCommand, InsurancePolicyDto>
{
    public Task<InsurancePolicyDto> Handle(UpdateInsurancePolicyCommand request, CancellationToken cancellationToken)
        => service.UpdateInsurancePolicyAsync(request, cancellationToken);
}
