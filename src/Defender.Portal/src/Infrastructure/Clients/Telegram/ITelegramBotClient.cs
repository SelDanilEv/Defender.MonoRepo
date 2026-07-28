namespace Defender.Portal.Infrastructure.Clients.Telegram;

public interface ITelegramBotClient
{
    Task<TelegramBotMessage> SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
}
