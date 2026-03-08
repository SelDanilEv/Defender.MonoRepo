using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdviser.Application.Modules.MenuSessions.Commands;

public record ConfirmMenuSessionCommand : IRequest<MenuSession?>
{
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }
    public IReadOnlyList<string> ConfirmedItems { get; init; } = [];
    public bool TrySomethingNew { get; init; }
}

public sealed class ConfirmMenuSessionCommandValidator : AbstractValidator<ConfirmMenuSessionCommand>
{
    public ConfirmMenuSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ConfirmedItems).NotNull();
    }
}

public sealed class ConfirmMenuSessionCommandHandler(IMenuSessionService menuSessionService)
    : IRequestHandler<ConfirmMenuSessionCommand, MenuSession?>
{
    public Task<MenuSession?> Handle(ConfirmMenuSessionCommand request, CancellationToken cancellationToken)
    {
        return menuSessionService.ConfirmAsync(
            request.SessionId,
            request.UserId,
            request.ConfirmedItems,
            request.TrySomethingNew,
            cancellationToken);
    }
}
