using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Common.Interfaces.Services;
using Defender.CarService.Application.Requests.Maintenance;
using Defender.CarService.Application.Services;
using MediatR;

namespace Defender.CarService.Application.Handlers.Maintenance;

public sealed class GetMaintenanceItemsQueryHandler(IMyGarageApplicationService service)
    : IRequestHandler<GetMaintenanceItemsQuery, IReadOnlyList<MaintenanceItemDto>>
{
    public Task<IReadOnlyList<MaintenanceItemDto>> Handle(GetMaintenanceItemsQuery request, CancellationToken cancellationToken)
        => service.GetMaintenanceItemsAsync(request.VehicleId, cancellationToken);
}

public sealed class CreateMaintenanceItemCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<CreateMaintenanceItemCommand, MaintenanceItemDto>
{
    public Task<MaintenanceItemDto> Handle(CreateMaintenanceItemCommand request, CancellationToken cancellationToken)
        => service.CreateMaintenanceItemAsync(request, cancellationToken);
}

public sealed class UpdateMaintenanceItemCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<UpdateMaintenanceItemCommand, MaintenanceItemDto>
{
    public Task<MaintenanceItemDto> Handle(UpdateMaintenanceItemCommand request, CancellationToken cancellationToken)
        => service.UpdateMaintenanceItemAsync(request, cancellationToken);
}

public sealed class DeleteMaintenanceItemCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<DeleteMaintenanceItemCommand, Unit>
{
    public Task<Unit> Handle(DeleteMaintenanceItemCommand request, CancellationToken cancellationToken)
        => service.DeleteMaintenanceItemAsync(request, cancellationToken);
}
