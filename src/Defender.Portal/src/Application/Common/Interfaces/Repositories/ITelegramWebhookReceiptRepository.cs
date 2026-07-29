using Defender.Portal.Application.Modules.Telegram;

namespace Defender.Portal.Application.Common.Interfaces.Repositories;

public interface ITelegramWebhookReceiptRepository
{
    Task<bool> TryCreateAsync(TelegramWebhookReceipt receipt, CancellationToken cancellationToken);
}
