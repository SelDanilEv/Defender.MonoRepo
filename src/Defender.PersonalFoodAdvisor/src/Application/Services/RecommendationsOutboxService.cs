using System.Net;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Application.Kafka;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdvisor.Application.Services;

public class RecommendationsOutboxService(
    IRecommendationsOutboxRepository repository,
    ILogger<RecommendationsOutboxService> logger) : IRecommendationsOutboxService
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(3),
        TimeSpan.FromMinutes(4),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(6),
        TimeSpan.FromMinutes(7),
        TimeSpan.FromMinutes(8),
        TimeSpan.FromMinutes(9),
        TimeSpan.FromMinutes(10)
    ];

    public async Task EnqueueAsync(RecommendationsRequestedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new RecommendationsOutboxMessage
        {
            SessionId = evt.SessionId,
            UserId = evt.UserId,
            ConfirmedItems = [.. evt.ConfirmedItems],
            TrySomethingNew = evt.TrySomethingNew,
            Attempt = evt.Attempt,
            NextAttemptAtUtc = DateTime.UtcNow
        };

        await repository.EnqueueAsync(message, cancellationToken);
        logger.LogInformation(
            "Enqueued recommendations outbox message for session {SessionId}, attempt {Attempt}",
            evt.SessionId,
            evt.Attempt);
    }

    public async Task<bool> ScheduleRetryAsync(
        RecommendationsRequestedEvent evt,
        HttpStatusCode? statusCode,
        CancellationToken cancellationToken = default)
    {
        if (evt.Attempt >= RetryDelays.Length)
        {
            logger.LogWarning(
                "Skipping automatic retry for session {SessionId}: max retry count reached at attempt {Attempt}",
                evt.SessionId,
                evt.Attempt);
            return false;
        }

        var nextAttempt = evt.Attempt + 1;
        var delay = RetryDelays[evt.Attempt];
        var message = new RecommendationsOutboxMessage
        {
            SessionId = evt.SessionId,
            UserId = evt.UserId,
            ConfirmedItems = [.. evt.ConfirmedItems],
            TrySomethingNew = evt.TrySomethingNew,
            Attempt = nextAttempt,
            NextAttemptAtUtc = DateTime.UtcNow.Add(delay),
            LastError = statusCode?.ToString()
        };

        await repository.EnqueueAsync(message, cancellationToken);
        logger.LogWarning(
            "Scheduled automatic recommendations retry for session {SessionId}: nextAttempt {Attempt}, delaySeconds {DelaySeconds}, statusCode {StatusCode}",
            evt.SessionId,
            nextAttempt,
            delay.TotalSeconds,
            statusCode);
        return true;
    }
}
