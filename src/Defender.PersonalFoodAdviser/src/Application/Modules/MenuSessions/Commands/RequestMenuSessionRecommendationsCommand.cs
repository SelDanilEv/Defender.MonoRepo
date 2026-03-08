using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdviser.Application.Modules.MenuSessions.Commands;

public record RequestMenuSessionRecommendationsCommand : IRequest<bool>
{
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }
}

public sealed class RequestMenuSessionRecommendationsCommandValidator : AbstractValidator<RequestMenuSessionRecommendationsCommand>
{
    public RequestMenuSessionRecommendationsCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class RequestMenuSessionRecommendationsCommandHandler(IMenuSessionService menuSessionService)
    : IRequestHandler<RequestMenuSessionRecommendationsCommand, bool>
{
    public async Task<bool> Handle(RequestMenuSessionRecommendationsCommand request, CancellationToken cancellationToken)
    {
        var session = await menuSessionService.GetByIdAsync(request.SessionId, request.UserId, cancellationToken);
        if (session == null)
        {
            return false;
        }

        await menuSessionService.RequestRecommendationsAsync(request.SessionId, request.UserId, cancellationToken);
        return true;
    }
}
