using Defender.JobSchedulerService.Application.Common.Interfaces.Repositories;
using Defender.JobSchedulerService.Application.Services;
using Defender.JobSchedulerService.Domain.Entities;
using Defender.Kafka.Default;

namespace Defender.JobSchedulerService.Tests.Services;

public class JobManagementServiceTests
{
    private readonly Mock<IDefaultKafkaProducer<string>> _kafkaProducer = new();
    private readonly Mock<IScheduledJobRepository> _scheduledJobRepository = new();

    [Fact]
    public async Task RunJobAsync_WhenScheduleIsNotDue_DoesNotProduceOrUpdate()
    {
        var job = new ScheduledJob
        {
            Id = Guid.NewGuid(),
            Topic = "jobs-topic",
            Event = "{\"x\":1}",
            Schedule = new Schedule
            {
                NextStartTime = DateTime.UtcNow.AddMinutes(5),
                EachMinutes = 1
            }
        };
        var sut = new JobManagementService(_kafkaProducer.Object, _scheduledJobRepository.Object);

        await sut.RunJobAsync(job);

        _kafkaProducer.Verify(
            x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _scheduledJobRepository.Verify(
            x => x.UpdateScheduledJobAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<ScheduledJob>>()),
            Times.Never);
    }

    [Fact]
    public async Task RunJobAsync_WhenScheduleIsDue_ProducesAndUpdates()
    {
        var job = new ScheduledJob
        {
            Id = Guid.NewGuid(),
            Topic = "jobs-topic",
            Event = "{\"x\":1}",
            Schedule = new Schedule
            {
                NextStartTime = DateTime.UtcNow.AddMinutes(-2),
                EachMinutes = 1
            }
        };
        _kafkaProducer
            .Setup(x => x.ProduceAsync(job.Topic, job.Event, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _scheduledJobRepository
            .Setup(x => x.UpdateScheduledJobAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<ScheduledJob>>()))
            .ReturnsAsync(job);
        var sut = new JobManagementService(_kafkaProducer.Object, _scheduledJobRepository.Object);

        await sut.RunJobAsync(job);

        _kafkaProducer.Verify(
            x => x.ProduceAsync(job.Topic, job.Event, It.IsAny<CancellationToken>()),
            Times.Once);
        _scheduledJobRepository.Verify(
            x => x.UpdateScheduledJobAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<ScheduledJob>>()),
            Times.Once);
    }
}
