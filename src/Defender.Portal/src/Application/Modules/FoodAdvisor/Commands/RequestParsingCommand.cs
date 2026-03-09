using Defender.Portal.Application.Common.Interfaces.Wrappers;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdvisor.Commands;

public record RequestParsingCommand(Guid SessionId) : IRequest;

public class RequestParsingCommandHandler(IPersonalFoodAdvisorWrapper wrapper)
    : IRequestHandler<RequestParsingCommand>
{
    public Task Handle(RequestParsingCommand request, CancellationToken cancellationToken)
        => wrapper.RequestParsingAsync(request.SessionId, cancellationToken);
}
