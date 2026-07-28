using Defender.Portal.Application.Common.Interfaces.Repositories;

namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramWebhookService(
    ITelegramWebhookReceiptRepository webhookReceiptRepository,
    ITelegramBotClient telegramBotClient,
    TimeProvider timeProvider) : ITelegramWebhookService
{
    public Task<bool> TryRecordAsync(long updateId, CancellationToken cancellationToken) =>
        webhookReceiptRepository.TryCreateAsync(
            new TelegramWebhookReceipt(updateId, timeProvider.GetUtcNow()),
            cancellationToken);

    public async Task HandleAsync(TelegramWebhookUpdate update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        if (message is null || message.ChatId <= 0 || string.IsNullOrWhiteSpace(message.Text))
        {
            return;
        }

        var command = message.Text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Split('@')[0].ToLowerInvariant();
        var reply = command switch
        {
            "/start" or "/app" => $"Open Defender Mini App: {MiniAppUrl}",
            "/help" => $"Commands: /app, /privacy, /unlink. Open Defender: {MiniAppUrl}",
            "/privacy" => $"Defender stores Telegram link and webhook receipts only for this integration. Manage access in Mini App: {MiniAppUrl}",
            "/unlink" => $"Open Mini App, then choose unlink in Telegram settings: {MiniAppUrl}",
            _ => null,
        };

        if (reply is not null)
        {
            await telegramBotClient.SendMessageAsync(message.ChatId, reply, cancellationToken);
        }
    }

    private const string MiniAppUrl = "https://portal.coded-by-danil.dev/telegram";
}
