using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdvisor;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdvisor.Queries;

public record GetPreferencesQuery : IRequest<PortalPreferencesDto?>;

public class GetPreferencesQueryHandler(IPersonalFoodAdvisorWrapper wrapper)
    : IRequestHandler<GetPreferencesQuery, PortalPreferencesDto?>
{
    public Task<PortalPreferencesDto?> Handle(GetPreferencesQuery request, CancellationToken cancellationToken)
        => wrapper.GetPreferencesAsync(cancellationToken);
}
