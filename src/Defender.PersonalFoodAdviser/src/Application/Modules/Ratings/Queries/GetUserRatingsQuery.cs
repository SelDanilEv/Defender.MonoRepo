using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdviser.Application.Modules.Ratings.Queries;

public record GetUserRatingsQuery : IRequest<IReadOnlyList<DishRating>>
{
    public Guid UserId { get; init; }
}

public sealed class GetUserRatingsQueryValidator : AbstractValidator<GetUserRatingsQuery>
{
    public GetUserRatingsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class GetUserRatingsQueryHandler(IRatingService ratingService)
    : IRequestHandler<GetUserRatingsQuery, IReadOnlyList<DishRating>>
{
    public Task<IReadOnlyList<DishRating>> Handle(GetUserRatingsQuery request, CancellationToken cancellationToken)
    {
        return ratingService.GetRatingsAsync(request.UserId, cancellationToken);
    }
}
