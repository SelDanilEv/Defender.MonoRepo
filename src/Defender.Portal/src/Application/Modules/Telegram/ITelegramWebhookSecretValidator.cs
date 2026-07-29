namespace Defender.Portal.Application.Modules.Telegram;

public interface ITelegramWebhookSecretValidator
{
    void Validate(string? secret);
}
