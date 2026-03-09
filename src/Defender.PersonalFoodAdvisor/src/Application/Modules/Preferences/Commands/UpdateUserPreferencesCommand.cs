using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdvisor.Application.Modules.Preferences.Commands;

public record UpdateUserPreferencesCommand : IRequest<UserPreferences>
{
    public Guid UserId { get; init; }
    public IReadOnlyList<string> Likes { get; init; } = [];
    public IReadOnlyList<string> Dislikes { get; init; } = [];
}

public sealed class UpdateUserPreferencesCommandValidator : AbstractValidator<UpdateUserPreferencesCommand>
{
    public UpdateUserPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Likes).NotNull();
        RuleFor(x => x.Dislikes).NotNull();
    }
}

public sealed class UpdateUserPreferencesCommandHandler(IPreferencesService preferencesService)
    : IRequestHandler<UpdateUserPreferencesCommand, UserPreferences>
{
    public Task<UserPreferences> Handle(UpdateUserPreferencesCommand request, CancellationToken cancellationToken)
    {
        return preferencesService.UpdateAsync(request.UserId, request.Likes, request.Dislikes, cancellationToken);
    }
}
