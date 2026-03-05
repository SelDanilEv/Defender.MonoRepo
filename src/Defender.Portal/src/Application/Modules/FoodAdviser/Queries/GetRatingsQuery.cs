using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.FoodAdviser;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdviser.Queries;

public record GetRatingsQuery : IRequest<IReadOnlyList<PortalDishRatingDto>>;

public class GetRatingsQueryHandler(IPersonalFoodAdviserWrapper wrapper)
    : IRequestHandler<GetRatingsQuery, IReadOnlyList<PortalDishRatingDto>>
{
    public Task<IReadOnlyList<PortalDishRatingDto>> Handle(GetRatingsQuery request, CancellationToken cancellationToken)
        => wrapper.GetRatingsAsync(cancellationToken);
}
