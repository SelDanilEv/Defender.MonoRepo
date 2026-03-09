using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdvisor.Application.Modules.MenuSessions.Queries;

public record GetMenuSessionByIdQuery : IRequest<MenuSession?>
{
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }
}

public sealed class GetMenuSessionByIdQueryValidator : AbstractValidator<GetMenuSessionByIdQuery>
{
    public GetMenuSessionByIdQueryValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class GetMenuSessionByIdQueryHandler(IMenuSessionService menuSessionService)
    : IRequestHandler<GetMenuSessionByIdQuery, MenuSession?>
{
    public Task<MenuSession?> Handle(GetMenuSessionByIdQuery request, CancellationToken cancellationToken)
    {
        return menuSessionService.GetByIdAsync(request.SessionId, request.UserId, cancellationToken);
    }
}
