using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdviser;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdviser.Queries;

public record GetSessionsQuery : IRequest<IReadOnlyList<PortalMenuSessionDto>>;

public class GetSessionsQueryHandler(IPersonalFoodAdviserWrapper wrapper)
    : IRequestHandler<GetSessionsQuery, IReadOnlyList<PortalMenuSessionDto>>
{
    public Task<IReadOnlyList<PortalMenuSessionDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
        => wrapper.GetSessionsAsync(cancellationToken);
}
