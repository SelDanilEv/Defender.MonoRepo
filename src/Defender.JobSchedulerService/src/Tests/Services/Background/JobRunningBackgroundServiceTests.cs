using Defender.JobSchedulerService.Application.Common.Interfaces.Services;
using Defender.JobSchedulerService.Application.Configuration.Options;
using Defender.JobSchedulerService.Application.Services.Background;
using Defender.JobSchedulerService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.JobSchedulerService.Tests.Services.Background;

public class JobRunningBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRunJobFails_LogsAndContinuesUntilCancellation()
    {
        var job = new ScheduledJob
        {
            Id = Guid.NewGuid(),
            Name = "nightly-job",
            Topic = "jobs-topic",
            Event = "{\"x\":1}",
            Schedule = new Schedule
            {
                NextStartTime = DateTime.UtcNow.AddMinutes(-1),
                EachMinutes = 1
            }
        };
        var options = Options.Create(new JobRunningOptions { LoopDelayMs = 1 });
        var logger = Mock.Of<ILogger<JobRunningBackgroundService>>();
        var jobRunningService = new Mock<IJobRunningService>();
        var cts = new CancellationTokenSource();

        jobRunningService
            .Setup(x => x.GetJobsToRunAsync())
            .ReturnsAsync(new[] { job });
        jobRunningService
            .Setup(x => x.RunJobAsync(job, false))
            .ThrowsAsync(new InvalidOperationException("publish failed"))
            .Callback(() => cts.Cancel());

        var sut = new TestJobRunningBackgroundService(options, logger, jobRunningService.Object);

        await sut.RunAsync(cts.Token);

        jobRunningService.Verify(x => x.GetJobsToRunAsync(), Times.Once);
        jobRunningService.Verify(x => x.RunJobAsync(job, false), Times.Once);
    }

    private sealed class TestJobRunningBackgroundService(
        IOptions<JobRunningOptions> options,
        ILogger<JobRunningBackgroundService> logger,
        IJobRunningService jobRunningService)
        : JobRunningBackgroundService(options, logger, jobRunningService)
    {
        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    }
}
