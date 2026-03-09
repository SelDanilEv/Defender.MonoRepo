using Defender.Portal.Application.Common.Interfaces.Wrappers;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdvisor.Queries;

public record GetRecommendationsQuery(Guid SessionId) : IRequest<IReadOnlyList<string>?>;

public class GetRecommendationsQueryHandler(IPersonalFoodAdvisorWrapper wrapper)
    : IRequestHandler<GetRecommendationsQuery, IReadOnlyList<string>?>
{
    public Task<IReadOnlyList<string>?> Handle(GetRecommendationsQuery request, CancellationToken cancellationToken)
        => wrapper.GetRecommendationsAsync(request.SessionId, cancellationToken);
}
