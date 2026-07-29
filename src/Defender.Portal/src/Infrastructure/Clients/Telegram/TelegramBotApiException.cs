using System.Net;

namespace Defender.Portal.Infrastructure.Clients.Telegram;

public class TelegramBotApiException(HttpStatusCode statusCode) : Exception($"Telegram Bot API returned HTTP {(int)statusCode}.")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
