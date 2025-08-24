using Defender.Common.Errors;
using Defender.Common.Extension;
using Defender.JobSchedulerService.Application.Common.Interfaces.Services;
using Defender.JobSchedulerService.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Defender.JobSchedulerService.Application.Modules.Jobs.Commands;

public record UpdateJobCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Topic { get; set; } = String.Empty;
    public string? Event { get; set; } = String.Empty;
    public DateTime StartDateTime { get; set; }
    public int EachMinutes { get; set; }
    public int EachHour { get; set; }
};

public sealed class UpdateJobCommandValidator : AbstractValidator<UpdateJobCommand>
{
    public UpdateJobCommandValidator()
    {
        RuleFor(s => s.Id)
            .NotEmpty()
            .NotNull()
            .WithMessage(ErrorCode.VL_InvalidRequest);

        RuleFor(s => s.Name)
            .NotEmpty()
            .NotNull()
            .WithMessage(ErrorCode.VL_InvalidRequest);

        RuleFor(command => command)
            .Must(command => command.EachMinutes > 0 || command.EachHour > 0)
            .WithMessage(ErrorCode.VL_InvalidRequest);

        //RuleFor(command => command.StartDateTime)
        //    .Must(BeInFuture)
        //    .WithMessage(ErrorCode.VL_InvalidRequest);
    }

    private static bool BeInFuture(DateTime startDateTime)
    {
        return startDateTime > DateTime.UtcNow;
    }
}

public sealed class UpdateJobCommandHandler(
        IJobManagementService accountManagementService)
    : IRequestHandler<UpdateJobCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateJobCommand request,
        CancellationToken cancellationToken)
    {
        var job = new ScheduledJob
        {
            Id = request.Id,
            Name = request.Name,
            Topic = request.Topic,
            Event = request.Event
        };

        job.AddSchedule(request.StartDateTime, request.EachMinutes, request.EachHour);

        await accountManagementService.UpdateJobAsync(job);

        return Unit.Value;
    }
}
