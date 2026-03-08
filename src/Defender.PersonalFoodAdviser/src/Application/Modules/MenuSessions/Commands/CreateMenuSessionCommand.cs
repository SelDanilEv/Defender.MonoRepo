using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdviser.Application.Modules.MenuSessions.Commands;

public record CreateMenuSessionCommand : IRequest<MenuSession>
{
    public Guid UserId { get; init; }
}

public sealed class CreateMenuSessionCommandValidator : AbstractValidator<CreateMenuSessionCommand>
{
    public CreateMenuSessionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class CreateMenuSessionCommandHandler(IMenuSessionService menuSessionService)
    : IRequestHandler<CreateMenuSessionCommand, MenuSession>
{
    public Task<MenuSession> Handle(CreateMenuSessionCommand request, CancellationToken cancellationToken)
    {
        return menuSessionService.CreateAsync(request.UserId, cancellationToken);
    }
}
