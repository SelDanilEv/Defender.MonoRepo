namespace Defender.Portal.Application.Modules.Telegram;

public interface ITelegramWebhookService
{
    Task<bool> TryRecordAsync(long updateId, CancellationToken cancellationToken);

    Task HandleAsync(TelegramWebhookUpdate update, CancellationToken cancellationToken);
}
