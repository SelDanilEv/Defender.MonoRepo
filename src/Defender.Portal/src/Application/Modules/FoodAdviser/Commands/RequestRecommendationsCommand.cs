using Defender.Portal.Application.Common.Interfaces.Wrappers;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdviser.Commands;

public record RequestRecommendationsCommand(Guid SessionId) : IRequest;

public class RequestRecommendationsCommandHandler(IPersonalFoodAdviserWrapper wrapper)
    : IRequestHandler<RequestRecommendationsCommand>
{
    public Task Handle(RequestRecommendationsCommand request, CancellationToken cancellationToken)
        => wrapper.RequestRecommendationsAsync(request.SessionId, cancellationToken);
}
