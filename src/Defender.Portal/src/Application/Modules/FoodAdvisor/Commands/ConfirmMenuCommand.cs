using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdvisor;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdvisor.Commands;

public record ConfirmMenuCommand(Guid SessionId, IReadOnlyList<string> ConfirmedItems, bool TrySomethingNew) : IRequest<PortalMenuSessionDto?>;

public class ConfirmMenuCommandHandler(IPersonalFoodAdvisorWrapper wrapper)
    : IRequestHandler<ConfirmMenuCommand, PortalMenuSessionDto?>
{
    public Task<PortalMenuSessionDto?> Handle(ConfirmMenuCommand request, CancellationToken cancellationToken)
        => wrapper.ConfirmMenuAsync(request.SessionId, request.ConfirmedItems, request.TrySomethingNew, cancellationToken);
}
