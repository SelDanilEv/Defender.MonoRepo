using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdviser.Application.Services.Background.Kafka;

public class RecommendationsOutboxPublisherService(
    IRecommendationsOutboxRepository repository,
    IRecommendationsKafkaPublisher publisher,
    ILogger<RecommendationsOutboxPublisherService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PublishFailureDelay = TimeSpan.FromMinutes(1);
    private readonly Guid _handlerId = Guid.NewGuid();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Starting RecommendationsOutboxPublisherService with handler {HandlerId} for topic {Topic}",
            _handlerId,
            KafkaTopicNames.RecommendationsRequested);

        while (!stoppingToken.IsCancellationRequested)
        {
            var processedAny = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                var message = await repository.ClaimNextDueAsync(
                    _handlerId,
                    DateTime.UtcNow,
                    LockDuration,
                    stoppingToken);

                if (message == null)
                    break;

                processedAny = true;

                try
                {
                    var evt = new RecommendationsRequestedEvent(
                        message.SessionId,
                        message.UserId,
                        message.ConfirmedItems,
                        message.TrySomethingNew,
                        message.Attempt);

                    await publisher.PublishAsync(evt, stoppingToken);
                    await repository.CompleteAsync(message.Id, _handlerId, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to publish recommendations outbox message {OutboxId} for session {SessionId}",
                        message.Id,
                        message.SessionId);

                    await repository.ReleaseAsync(
                        message.Id,
                        _handlerId,
                        DateTime.UtcNow.Add(PublishFailureDelay),
                        ex.Message,
                        stoppingToken);
                }
            }

            await Task.Delay(processedAny ? TimeSpan.FromMilliseconds(250) : PollInterval, stoppingToken);
        }
    }
}
