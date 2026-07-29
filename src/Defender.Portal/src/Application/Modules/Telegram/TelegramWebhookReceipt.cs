namespace Defender.Portal.Application.Modules.Telegram;

public sealed record TelegramWebhookReceipt(long UpdateId, DateTimeOffset ReceivedAt);
