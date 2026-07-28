namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramWebhookValidationException() : Exception("Telegram webhook secret is invalid.");
