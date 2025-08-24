using Defender.JobSchedulerService.Domain.Entities;

namespace Defender.JobSchedulerService.Application.Common.Interfaces.Services;

public interface IJobRunningService
{
    Task<ICollection<ScheduledJob>> GetJobsToRunAsync();
    Task RunJobAsync(ScheduledJob scheduledJob, bool force = false);
}
