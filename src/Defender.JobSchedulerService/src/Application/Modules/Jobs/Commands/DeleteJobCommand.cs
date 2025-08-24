using Defender.Common.Errors;
using Defender.Common.Extension;
using Defender.JobSchedulerService.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.JobSchedulerService.Application.Modules.Jobs.Commands;

public record DeleteJobCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
};

public sealed class DeleteJobCommandValidator : AbstractValidator<DeleteJobCommand>
{
    public DeleteJobCommandValidator()
    {
        RuleFor(s => s.Id)
          .NotEmpty()
          .NotNull()
          .WithMessage(ErrorCode.VL_InvalidRequest);
    }
}

public sealed class DeleteJobCommandHandler(
        IJobManagementService accountManagementService)
    : IRequestHandler<DeleteJobCommand, Unit>
{
    public async Task<Unit> Handle(DeleteJobCommand request, CancellationToken cancellationToken)
    {
        await accountManagementService.DeleteJobAsync(request.Id);

        return Unit.Value;
    }
}
