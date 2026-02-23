using Defender.Portal.Application.Common.Interfaces.Wrappers;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdviser.Commands;

public record SubmitRatingCommand(string DishName, int Rating, Guid? SessionId) : IRequest;

public class SubmitRatingCommandHandler(IPersonalFoodAdviserWrapper wrapper)
    : IRequestHandler<SubmitRatingCommand>
{
    public Task Handle(SubmitRatingCommand request, CancellationToken cancellationToken)
        => wrapper.SubmitRatingAsync(request.DishName, request.Rating, request.SessionId, cancellationToken);
}
