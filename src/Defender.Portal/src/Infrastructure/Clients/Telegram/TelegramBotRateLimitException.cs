using System.Net;

namespace Defender.Portal.Infrastructure.Clients.Telegram;

public sealed class TelegramBotRateLimitException(TimeSpan retryAfter) : TelegramBotApiException((HttpStatusCode)429)
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}
