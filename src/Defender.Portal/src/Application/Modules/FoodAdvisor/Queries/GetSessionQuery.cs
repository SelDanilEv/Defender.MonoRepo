using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdvisor;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdvisor.Queries;

public record GetSessionQuery(Guid SessionId) : IRequest<PortalMenuSessionDto?>;

public class GetSessionQueryHandler(IPersonalFoodAdvisorWrapper wrapper)
    : IRequestHandler<GetSessionQuery, PortalMenuSessionDto?>
{
    public Task<PortalMenuSessionDto?> Handle(GetSessionQuery request, CancellationToken cancellationToken)
        => wrapper.GetSessionAsync(request.SessionId, cancellationToken);
}
