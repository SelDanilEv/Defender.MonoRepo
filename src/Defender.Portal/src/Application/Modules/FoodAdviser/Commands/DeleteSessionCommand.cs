using Defender.Portal.Application.Common.Interfaces.Wrappers;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdviser.Commands;

public record DeleteSessionCommand(Guid SessionId) : IRequest<bool>;

public class DeleteSessionCommandHandler(IPersonalFoodAdviserWrapper wrapper)
    : IRequestHandler<DeleteSessionCommand, bool>
{
    public Task<bool> Handle(DeleteSessionCommand request, CancellationToken cancellationToken)
        => wrapper.DeleteSessionAsync(request.SessionId, cancellationToken);
}
