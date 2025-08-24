using Defender.Common.Errors;
using Defender.Common.Extension;
using Defender.JobSchedulerService.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Defender.JobSchedulerService.Application.Modules.Jobs.Commands;

public record StartJobCommand : IRequest<Unit>
{
    public string? Name { get; set; }
};

public sealed class StartJobCommandValidator : AbstractValidator<StartJobCommand>
{
    public StartJobCommandValidator()
    {
        RuleFor(s => s.Name)
            .NotEmpty()
            .NotNull()
            .WithMessage(ErrorCode.VL_InvalidRequest);
    }
}

public sealed class StartJobCommandHandler(
        IJobManagementService jobManagementService,
        IJobRunningService jobRunningService)
    : IRequestHandler<StartJobCommand, Unit>
{
    public async Task<Unit> Handle(StartJobCommand request, CancellationToken cancellationToken)
    {
        var job = await jobManagementService.GetJobsAsync(
            new Defender.Common.DB.Pagination.PaginationRequest(),
            request.Name);

        if (job == null)
        {
            return Unit.Value;
        }

        foreach (var item in job.Items)
        {
            await jobRunningService.RunJobAsync(item, true);
        }

        return Unit.Value;
    }
}
