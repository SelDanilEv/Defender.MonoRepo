using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.History;

public sealed record DeleteHistoryCommand : IRequest<Unit>
{
    public Guid VehicleId { get; init; }

    public Guid HistoryId { get; init; }
}

public sealed class DeleteHistoryCommandValidator : AbstractValidator<DeleteHistoryCommand>
{
    public DeleteHistoryCommandValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
        RuleFor(request => request.HistoryId)
            .NotEmpty()
            .WithMessage("CAR_HISTORY_NOT_FOUND");
    }
}
