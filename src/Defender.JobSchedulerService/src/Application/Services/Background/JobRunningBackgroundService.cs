using Defender.JobSchedulerService.Application.Common.Interfaces.Services;
using Defender.JobSchedulerService.Application.Configuration.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.JobSchedulerService.Application.Services.Background;

public class JobRunningBackgroundService(
    IOptions<JobRunningOptions> options,
    ILogger<JobRunningBackgroundService> logger,
    IJobRunningService jobRunningService)
    : BackgroundService
{
    private readonly int _loopDelayMs = options.Value.LoopDelayMs;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_loopDelayMs, stoppingToken);

                var jobsToRun = await jobRunningService.GetJobsToRunAsync();

                await Parallel.ForEachAsync(jobsToRun, stoppingToken, async (job, cancellationToken) =>
                {
                    try
                    {
                        await jobRunningService.RunJobAsync(job);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Failed to run scheduled job {JobId} ({JobName}) for topic {Topic}. The job will be retried on the next loop because the schedule was not persisted.",
                            job.Id,
                            job.Name,
                            job.Topic);
                    }
                });
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while polling scheduled jobs.");
            }
        }
    }
}
