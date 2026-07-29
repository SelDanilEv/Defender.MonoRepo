using System.Net;

namespace Defender.Portal.Infrastructure.Clients.Telegram;

public sealed class TelegramBotForbiddenException(long chatId) : TelegramBotApiException(HttpStatusCode.Forbidden)
{
    public long ChatId { get; } = chatId;
}
