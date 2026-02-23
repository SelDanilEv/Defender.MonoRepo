using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdviser;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdviser.Queries;

public record GetPreferencesQuery : IRequest<PortalPreferencesDto?>;

public class GetPreferencesQueryHandler(IPersonalFoodAdviserWrapper wrapper)
    : IRequestHandler<GetPreferencesQuery, PortalPreferencesDto?>
{
    public Task<PortalPreferencesDto?> Handle(GetPreferencesQuery request, CancellationToken cancellationToken)
        => wrapper.GetPreferencesAsync(cancellationToken);
}
