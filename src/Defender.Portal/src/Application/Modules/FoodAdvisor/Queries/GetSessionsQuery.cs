using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdvisor;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdvisor.Queries;

public record GetSessionsQuery : IRequest<IReadOnlyList<PortalMenuSessionDto>>;

public class GetSessionsQueryHandler(IPersonalFoodAdvisorWrapper wrapper)
    : IRequestHandler<GetSessionsQuery, IReadOnlyList<PortalMenuSessionDto>>
{
    public Task<IReadOnlyList<PortalMenuSessionDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
        => wrapper.GetSessionsAsync(cancellationToken);
}
