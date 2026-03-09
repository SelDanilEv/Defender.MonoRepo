using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdvisor.Application.Modules.MenuSessions.Commands;

public record RequestMenuSessionParsingCommand : IRequest<bool>
{
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }
}

public sealed class RequestMenuSessionParsingCommandValidator : AbstractValidator<RequestMenuSessionParsingCommand>
{
    public RequestMenuSessionParsingCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class RequestMenuSessionParsingCommandHandler(IMenuSessionService menuSessionService)
    : IRequestHandler<RequestMenuSessionParsingCommand, bool>
{
    public async Task<bool> Handle(RequestMenuSessionParsingCommand request, CancellationToken cancellationToken)
    {
        var session = await menuSessionService.GetByIdAsync(request.SessionId, request.UserId, cancellationToken);
        if (session == null)
        {
            return false;
        }

        await menuSessionService.RequestParsingAsync(request.SessionId, request.UserId, cancellationToken);
        return true;
    }
}
