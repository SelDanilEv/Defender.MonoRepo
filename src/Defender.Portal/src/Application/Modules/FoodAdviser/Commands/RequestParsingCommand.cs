using Defender.Portal.Application.Common.Interfaces.Wrappers;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdviser.Commands;

public record RequestParsingCommand(Guid SessionId) : IRequest;

public class RequestParsingCommandHandler(IPersonalFoodAdviserWrapper wrapper)
    : IRequestHandler<RequestParsingCommand>
{
    public Task Handle(RequestParsingCommand request, CancellationToken cancellationToken)
        => wrapper.RequestParsingAsync(request.SessionId, cancellationToken);
}
