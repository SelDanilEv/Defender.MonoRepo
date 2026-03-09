using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdvisor;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdvisor.Commands;

public record CreateSessionCommand : IRequest<PortalMenuSessionDto>;

public class CreateSessionCommandHandler(IPersonalFoodAdvisorWrapper wrapper)
    : IRequestHandler<CreateSessionCommand, PortalMenuSessionDto>
{
    public Task<PortalMenuSessionDto> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
        => wrapper.CreateSessionAsync(cancellationToken);
}
