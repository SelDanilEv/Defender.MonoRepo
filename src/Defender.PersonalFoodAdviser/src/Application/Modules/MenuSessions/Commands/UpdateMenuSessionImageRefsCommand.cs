using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdviser.Application.Modules.MenuSessions.Commands;

public record UpdateMenuSessionImageRefsCommand : IRequest<MenuSession?>
{
    public Guid SessionId { get; init; }
    public Guid UserId { get; init; }
    public IReadOnlyList<string> ImageRefs { get; init; } = [];
}

public sealed class UpdateMenuSessionImageRefsCommandValidator : AbstractValidator<UpdateMenuSessionImageRefsCommand>
{
    public UpdateMenuSessionImageRefsCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ImageRefs).NotNull();
    }
}

public sealed class UpdateMenuSessionImageRefsCommandHandler(IMenuSessionService menuSessionService)
    : IRequestHandler<UpdateMenuSessionImageRefsCommand, MenuSession?>
{
    public Task<MenuSession?> Handle(UpdateMenuSessionImageRefsCommand request, CancellationToken cancellationToken)
    {
        return menuSessionService.UpdateImageRefsAsync(request.SessionId, request.UserId, request.ImageRefs, cancellationToken);
    }
}
