using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdviser.Application.Services;

public class MenuParsingOutboxService(
    IMenuParsingOutboxRepository repository,
    ILogger<MenuParsingOutboxService> logger) : IMenuParsingOutboxService
{
    public async Task EnqueueAsync(MenuParsingRequestedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new MenuParsingOutboxMessage
        {
            SessionId = evt.SessionId,
            UserId = evt.UserId,
            ImageRefs = [.. evt.ImageRefs],
            NextAttemptAtUtc = DateTime.UtcNow
        };

        await repository.EnqueueAsync(message, cancellationToken);
        logger.LogInformation(
            "Enqueued menu parsing outbox message for session {SessionId}, imageRefsCount {ImageRefsCount}",
            evt.SessionId,
            evt.ImageRefs.Count);
    }
}
