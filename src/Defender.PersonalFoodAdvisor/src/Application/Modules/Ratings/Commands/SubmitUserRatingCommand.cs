using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.PersonalFoodAdvisor.Application.Modules.Ratings.Commands;

public record SubmitUserRatingCommand : IRequest<Unit>
{
    public Guid UserId { get; init; }
    public string DishName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public Guid? SessionId { get; init; }
}

public sealed class SubmitUserRatingCommandValidator : AbstractValidator<SubmitUserRatingCommand>
{
    public SubmitUserRatingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DishName).NotNull();
    }
}

public sealed class SubmitUserRatingCommandHandler(IRatingService ratingService)
    : IRequestHandler<SubmitUserRatingCommand, Unit>
{
    public async Task<Unit> Handle(SubmitUserRatingCommand request, CancellationToken cancellationToken)
    {
        await ratingService.SubmitRatingAsync(
            request.UserId,
            request.DishName,
            request.Rating,
            request.SessionId,
            cancellationToken);

        return Unit.Value;
    }
}
