using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.History;

public sealed record GetHistoryQuery : IRequest<ServiceHistoryPageDto>
{
    public Guid VehicleId { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; } = 25;
}

public sealed class GetHistoryQueryValidator : AbstractValidator<GetHistoryQuery>
{
    public GetHistoryQueryValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("CAR_VEHICLE_NOT_FOUND");
        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CAR_HISTORY_PAGINATION_INVALID");
        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("CAR_HISTORY_PAGINATION_INVALID");
    }
}
