using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdvisor;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdvisor.Queries;

public record GetRatingsQuery : IRequest<IReadOnlyList<PortalDishRatingDto>>;

public class GetRatingsQueryHandler(IPersonalFoodAdvisorWrapper wrapper)
    : IRequestHandler<GetRatingsQuery, IReadOnlyList<PortalDishRatingDto>>
{
    public Task<IReadOnlyList<PortalDishRatingDto>> Handle(GetRatingsQuery request, CancellationToken cancellationToken)
        => wrapper.GetRatingsAsync(cancellationToken);
}
