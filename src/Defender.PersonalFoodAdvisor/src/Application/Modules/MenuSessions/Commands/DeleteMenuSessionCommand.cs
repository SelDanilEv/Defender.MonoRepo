using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdvisor.Application.Modules.MenuSessions.Commands;

public record DeleteMenuSessionCommand : IRequest<bool>
{
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }
}

public sealed class DeleteMenuSessionCommandValidator : AbstractValidator<DeleteMenuSessionCommand>
{
    public DeleteMenuSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class DeleteMenuSessionCommandHandler(IMenuSessionService menuSessionService)
    : IRequestHandler<DeleteMenuSessionCommand, bool>
{
    public Task<bool> Handle(DeleteMenuSessionCommand request, CancellationToken cancellationToken)
    {
        return menuSessionService.DeleteAsync(request.SessionId, request.UserId, cancellationToken);
    }
}
