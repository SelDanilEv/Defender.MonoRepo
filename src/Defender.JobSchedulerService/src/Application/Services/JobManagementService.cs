using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;
using Defender.JobSchedulerService.Application.Common.Interfaces.Repositories;
using Defender.JobSchedulerService.Application.Common.Interfaces.Services;
using Defender.JobSchedulerService.Domain.Entities;
using Defender.Kafka.Default;

namespace Defender.JobSchedulerService.Application.Services;

public class JobManagementService(
        IDefaultKafkaProducer<string> kafkaProducer,
        IScheduledJobRepository scheduledJobRepository)
    : IJobManagementService, IJobRunningService
{
    public async Task<ICollection<ScheduledJob>> GetJobsToRunAsync()
    {
        var settings = PaginationSettings<ScheduledJob>.WithoutPagination();

        settings.SetupFindOptions(
            FindModelRequest<ScheduledJob>
                .Init(x => x.Schedule.NextStartTime, DateTime.UtcNow, FilterType.Lt)
                .Sort(x => x.Schedule.NextStartTime, SortType.Desc));

        var pagedResult = await scheduledJobRepository.GetScheduledJobsAsync(settings);

        return pagedResult.Items;
    }

    public async Task<PagedResult<ScheduledJob>> GetJobsAsync(
        PaginationRequest paginationRequest, string name = "")
    {
        var settings = PaginationSettings<ScheduledJob>.FromPaginationRequest(paginationRequest);

        if (!String.IsNullOrWhiteSpace(name))
        {
            var filterRequest = FindModelRequest<ScheduledJob>
                .Init(x => x.Name, name)
                .Sort(x => x.Schedule.NextStartTime, SortType.Desc);

            settings.SetupFindOptions(filterRequest);
        }

        return await scheduledJobRepository.GetScheduledJobsAsync(settings);
    }

    public async Task<ScheduledJob> CreateJobAsync(ScheduledJob scheduledJob)
    {
        return await scheduledJobRepository.CreateScheduledJobAsync(scheduledJob);
    }

    public async Task<ScheduledJob> UpdateJobAsync(ScheduledJob scheduledJob)
    {
        var updateRequest = UpdateModelRequest<ScheduledJob>
            .Init(scheduledJob.Id)
            .Set(x => x.Name, scheduledJob.Name)
            .Set(x => x.Schedule, scheduledJob.Schedule);

        return await scheduledJobRepository.UpdateScheduledJobAsync(updateRequest);
    }

    public async Task DeleteJobAsync(Guid id)
    {
        await scheduledJobRepository.DeleteScheduledJobAsync(id);
    }

    public async Task RunJobAsync(ScheduledJob scheduledJob, bool force = false)
    {
        if (scheduledJob.ScheduleNextRun(force))
        {
            await kafkaProducer.ProduceAsync(
                scheduledJob.Topic,
                scheduledJob.Event,
                CancellationToken.None);

            var updateRequest = UpdateModelRequest<ScheduledJob>
                .Init(scheduledJob.Id)
                .Set(x => x.Schedule, scheduledJob.Schedule);

            await scheduledJobRepository.UpdateScheduledJobAsync(updateRequest);
        }
    }
}
