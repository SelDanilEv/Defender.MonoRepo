using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdviser.Application.Modules.MenuSessions.Queries;

public record GetMenuSessionRecommendationsQuery : IRequest<IReadOnlyList<string>?>
{
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }
}

public sealed class GetMenuSessionRecommendationsQueryValidator : AbstractValidator<GetMenuSessionRecommendationsQuery>
{
    public GetMenuSessionRecommendationsQueryValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class GetMenuSessionRecommendationsQueryHandler(IMenuSessionService menuSessionService)
    : IRequestHandler<GetMenuSessionRecommendationsQuery, IReadOnlyList<string>?>
{
    public Task<IReadOnlyList<string>?> Handle(GetMenuSessionRecommendationsQuery request, CancellationToken cancellationToken)
    {
        return menuSessionService.GetRecommendationsAsync(request.SessionId, request.UserId, cancellationToken);
    }
}
