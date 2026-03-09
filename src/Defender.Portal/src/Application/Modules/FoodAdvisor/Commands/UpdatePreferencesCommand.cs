using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdvisor;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdvisor.Commands;

public record UpdatePreferencesCommand(IReadOnlyList<string> Likes, IReadOnlyList<string> Dislikes) : IRequest<PortalPreferencesDto>;

public class UpdatePreferencesCommandHandler(IPersonalFoodAdvisorWrapper wrapper)
    : IRequestHandler<UpdatePreferencesCommand, PortalPreferencesDto>
{
    public Task<PortalPreferencesDto> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
        => wrapper.UpdatePreferencesAsync(request.Likes, request.Dislikes, cancellationToken);
}
