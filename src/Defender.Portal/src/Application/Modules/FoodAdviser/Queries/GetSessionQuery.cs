using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdviser;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdviser.Queries;

public record GetSessionQuery(Guid SessionId) : IRequest<PortalMenuSessionDto?>;

public class GetSessionQueryHandler(IPersonalFoodAdviserWrapper wrapper)
    : IRequestHandler<GetSessionQuery, PortalMenuSessionDto?>
{
    public Task<PortalMenuSessionDto?> Handle(GetSessionQuery request, CancellationToken cancellationToken)
        => wrapper.GetSessionAsync(request.SessionId, cancellationToken);
}
