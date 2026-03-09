using Defender.Portal.Application.Common.Interfaces.Wrappers;
using MediatR;

namespace Defender.Portal.Application.Modules.FoodAdvisor.Commands;

public record SubmitRatingCommand(string DishName, int Rating, Guid? SessionId) : IRequest;

public class SubmitRatingCommandHandler(IPersonalFoodAdvisorWrapper wrapper)
    : IRequestHandler<SubmitRatingCommand>
{
    public Task Handle(SubmitRatingCommand request, CancellationToken cancellationToken)
        => wrapper.SubmitRatingAsync(request.DishName, request.Rating, request.SessionId, cancellationToken);
}
