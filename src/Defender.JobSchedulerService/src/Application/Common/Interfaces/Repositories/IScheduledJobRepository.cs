using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;
using Defender.JobSchedulerService.Domain.Entities;

namespace Defender.JobSchedulerService.Application.Common.Interfaces.Repositories;

public interface IScheduledJobRepository
{
    Task<ScheduledJob> GetScheduledJobByIdAsync(Guid id);
    Task<PagedResult<ScheduledJob>> GetScheduledJobsAsync(
        PaginationSettings<ScheduledJob> settings);
    Task<ScheduledJob> CreateScheduledJobAsync(ScheduledJob job);
    Task<ScheduledJob> UpdateScheduledJobAsync(UpdateModelRequest<ScheduledJob> updateRequest);
    Task DeleteScheduledJobAsync(Guid id);

}
