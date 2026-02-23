using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdviser;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdviser.Commands;

public record CreateSessionCommand : IRequest<PortalMenuSessionDto>;

public class CreateSessionCommandHandler(IPersonalFoodAdviserWrapper wrapper)
    : IRequestHandler<CreateSessionCommand, PortalMenuSessionDto>
{
    public Task<PortalMenuSessionDto> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
        => wrapper.CreateSessionAsync(cancellationToken);
}
