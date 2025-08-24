using Defender.JobSchedulerService.Application.Common.Interfaces.Services;
using Defender.JobSchedulerService.Application.Configuration.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Defender.JobSchedulerService.Application.Services.Background;

public class JobRunningBackgroundService(
    IOptions<JobRunningOptions> options,
    IJobRunningService jobRunningService)
    : BackgroundService
{
    private readonly int _loopDelayMs = options.Value.LoopDelayMs;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_loopDelayMs, stoppingToken);

            var jobsToRun = await jobRunningService.GetJobsToRunAsync();

            await Parallel.ForEachAsync(jobsToRun, async (job, cancellationToken) =>
            {
                await jobRunningService.RunJobAsync(job);
            });
        }
    }
}
