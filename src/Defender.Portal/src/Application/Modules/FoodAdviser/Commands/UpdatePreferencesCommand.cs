using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdviser;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdviser.Commands;

public record UpdatePreferencesCommand(IReadOnlyList<string> Likes, IReadOnlyList<string> Dislikes) : IRequest<PortalPreferencesDto>;

public class UpdatePreferencesCommandHandler(IPersonalFoodAdviserWrapper wrapper)
    : IRequestHandler<UpdatePreferencesCommand, PortalPreferencesDto>
{
    public Task<PortalPreferencesDto> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
        => wrapper.UpdatePreferencesAsync(request.Likes, request.Dislikes, cancellationToken);
}
