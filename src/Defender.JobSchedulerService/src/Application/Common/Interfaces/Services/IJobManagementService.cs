using Defender.Common.DB.Pagination;
using Defender.JobSchedulerService.Domain.Entities;

namespace Defender.JobSchedulerService.Application.Common.Interfaces.Services;

public interface IJobManagementService
{
    Task<PagedResult<ScheduledJob>> GetJobsAsync(
        PaginationRequest paginationRequest, string name = "");
    Task<ScheduledJob> CreateJobAsync(ScheduledJob scheduledJob);
    Task<ScheduledJob> UpdateJobAsync(ScheduledJob scheduledJob);
    Task DeleteJobAsync(Guid id);
}
