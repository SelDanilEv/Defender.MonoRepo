using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdviser.Application.Modules.Preferences.Queries;

public record GetUserPreferencesQuery : IRequest<UserPreferences>
{
    public Guid UserId { get; init; }
}

public sealed class GetUserPreferencesQueryValidator : AbstractValidator<GetUserPreferencesQuery>
{
    public GetUserPreferencesQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class GetUserPreferencesQueryHandler(IPreferencesService preferencesService)
    : IRequestHandler<GetUserPreferencesQuery, UserPreferences>
{
    public Task<UserPreferences> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        return preferencesService.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}
