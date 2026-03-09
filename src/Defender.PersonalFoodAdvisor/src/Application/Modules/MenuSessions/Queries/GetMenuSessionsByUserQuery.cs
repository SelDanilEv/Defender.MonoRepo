using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdvisor.Application.Modules.MenuSessions.Queries;

public record GetMenuSessionsByUserQuery : IRequest<IReadOnlyList<MenuSession>>
{
    public Guid UserId { get; init; }
}

public sealed class GetMenuSessionsByUserQueryValidator : AbstractValidator<GetMenuSessionsByUserQuery>
{
    public GetMenuSessionsByUserQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class GetMenuSessionsByUserQueryHandler(IMenuSessionService menuSessionService)
    : IRequestHandler<GetMenuSessionsByUserQuery, IReadOnlyList<MenuSession>>
{
    public Task<IReadOnlyList<MenuSession>> Handle(GetMenuSessionsByUserQuery request, CancellationToken cancellationToken)
    {
        return menuSessionService.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}
