using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Common.Interfaces.Services;
using Defender.CarService.Application.Requests.Vehicles;
using Defender.CarService.Application.Services;
using MediatR;

namespace Defender.CarService.Application.Handlers.Vehicles;

public sealed class GetVehiclesQueryHandler(IMyGarageApplicationService service)
    : IRequestHandler<GetVehiclesQuery, IReadOnlyList<VehicleSummaryDto>>
{
    public Task<IReadOnlyList<VehicleSummaryDto>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
        => service.GetVehiclesAsync(request.IncludeArchived, cancellationToken);
}

public sealed class GetVehicleQueryHandler(IMyGarageApplicationService service)
    : IRequestHandler<GetVehicleQuery, VehicleDetailDto>
{
    public Task<VehicleDetailDto> Handle(GetVehicleQuery request, CancellationToken cancellationToken)
        => service.GetVehicleAsync(request.VehicleId, cancellationToken);
}

public sealed class CreateVehicleCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<CreateVehicleCommand, VehicleDto>
{
    public Task<VehicleDto> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
        => service.CreateVehicleAsync(request, cancellationToken);
}

public sealed class UpdateVehicleCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<UpdateVehicleCommand, VehicleDto>
{
    public Task<VehicleDto> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
        => service.UpdateVehicleAsync(request, cancellationToken);
}

public sealed class ArchiveVehicleCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<ArchiveVehicleCommand, VehicleDto>
{
    public Task<VehicleDto> Handle(ArchiveVehicleCommand request, CancellationToken cancellationToken)
        => service.ArchiveVehicleAsync(request.VehicleId, cancellationToken);
}

public sealed class UnarchiveVehicleCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<UnarchiveVehicleCommand, VehicleDto>
{
    public Task<VehicleDto> Handle(UnarchiveVehicleCommand request, CancellationToken cancellationToken)
        => service.UnarchiveVehicleAsync(request.VehicleId, cancellationToken);
}
