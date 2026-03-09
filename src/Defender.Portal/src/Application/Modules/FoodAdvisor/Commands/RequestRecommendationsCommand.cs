using Defender.Portal.Application.Common.Interfaces.Wrappers;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdvisor.Commands;

public record RequestRecommendationsCommand(Guid SessionId) : IRequest;

public class RequestRecommendationsCommandHandler(IPersonalFoodAdvisorWrapper wrapper)
    : IRequestHandler<RequestRecommendationsCommand>
{
    public Task Handle(RequestRecommendationsCommand request, CancellationToken cancellationToken)
        => wrapper.RequestRecommendationsAsync(request.SessionId, cancellationToken);
}
