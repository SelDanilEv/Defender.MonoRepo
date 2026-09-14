using Defender.CarService.Application.DTOs;
using FluentValidation;
using MediatR;

namespace Defender.CarService.Application.Requests.Maintenance;

public sealed record CreateMaintenanceItemCommand : IRequest<MaintenanceItemDto>
{
    public Guid VehicleId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int? IntervalMonths { get; init; }

    public int? IntervalThousandKm { get; init; }

    public DateOnly? LastDate { get; init; }

    public long? LastOdometerKm { get; init; }

    public DateOnly? ManualBaselineDate { get; init; }

    public long? ManualBaselineOdometerKm { get; init; }
}

public sealed class CreateMaintenanceItemCommandValidator : AbstractValidator<CreateMaintenanceItemCommand>
{
    public CreateMaintenanceItemCommandValidator()
    {
        MaintenanceRequestValidation.AddRules(this, request => request.VehicleId, request => request.Name, request => request.IntervalMonths, request => request.IntervalThousandKm, request => request.ManualBaselineDate, request => request.ManualBaselineOdometerKm);
    }
}
