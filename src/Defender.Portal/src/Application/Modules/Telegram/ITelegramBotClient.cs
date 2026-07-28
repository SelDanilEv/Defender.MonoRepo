namespace Defender.Portal.Application.Modules.Telegram;

public interface ITelegramBotClient
{
    Task<TelegramBotMessage> SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
}
