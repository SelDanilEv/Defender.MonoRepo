using Defender.Common.Configuration.Options;
using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;
using Defender.Common.DB.Repositories;
using Defender.JobSchedulerService.Application.Common.Interfaces.Repositories;
using Defender.JobSchedulerService.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Defender.JobSchedulerService.Infrastructure.Repositories;

public class ScheduledJobRepository : BaseMongoRepository<ScheduledJob>, IScheduledJobRepository
{
    public ScheduledJobRepository(IOptions<MongoDbOptions> mongoOption) : base(mongoOption.Value)
    {
    }

    public async Task<ScheduledJob> GetScheduledJobByIdAsync(Guid id)
    {
        return await GetItemAsync(id);
    }

    public async Task<PagedResult<ScheduledJob>> GetScheduledJobsAsync(
        PaginationSettings<ScheduledJob> settings)
    {
        return await GetItemsAsync(settings);
    }

    public async Task<ScheduledJob> CreateScheduledJobAsync(ScheduledJob job)
    {
        return await AddItemAsync(job);
    }

    public async Task<ScheduledJob> UpdateScheduledJobAsync(
        UpdateModelRequest<ScheduledJob> updateRequest)
    {
        return await UpdateItemAsync(updateRequest);
    }

    public async Task DeleteScheduledJobAsync(Guid id)
    {
        await RemoveItemAsync(id);
    }
}
