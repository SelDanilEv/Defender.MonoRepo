using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Common.Interfaces.Services;
using Defender.CarService.Application.Requests.History;
using Defender.CarService.Application.Services;
using MediatR;

namespace Defender.CarService.Application.Handlers.History;

public sealed class GetHistoryQueryHandler(IMyGarageApplicationService service)
    : IRequestHandler<GetHistoryQuery, ServiceHistoryPageDto>
{
    public Task<ServiceHistoryPageDto> Handle(GetHistoryQuery request, CancellationToken cancellationToken)
        => service.GetHistoryAsync(request, cancellationToken);
}

public sealed class CreateHistoryCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<CreateHistoryCommand, ServiceHistoryRecordDto>
{
    public Task<ServiceHistoryRecordDto> Handle(CreateHistoryCommand request, CancellationToken cancellationToken)
        => service.CreateHistoryAsync(request, cancellationToken);
}

public sealed class UpdateHistoryCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<UpdateHistoryCommand, ServiceHistoryRecordDto>
{
    public Task<ServiceHistoryRecordDto> Handle(UpdateHistoryCommand request, CancellationToken cancellationToken)
        => service.UpdateHistoryAsync(request, cancellationToken);
}

public sealed class DeleteHistoryCommandHandler(IMyGarageApplicationService service)
    : IRequestHandler<DeleteHistoryCommand, Unit>
{
    public Task<Unit> Handle(DeleteHistoryCommand request, CancellationToken cancellationToken)
        => service.DeleteHistoryAsync(request, cancellationToken);
}
