using System.Text.Json.Serialization;

namespace Defender.Portal.Application.Modules.Telegram;

public sealed record TelegramWebhookUpdate(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TelegramWebhookMessage? Message = null);

public sealed record TelegramWebhookMessage(
    [property: JsonPropertyName("chat")] TelegramWebhookChat? Chat,
    [property: JsonPropertyName("text")] string? Text)
{
    public long ChatId => Chat?.Id ?? 0;
}

public sealed record TelegramWebhookChat([property: JsonPropertyName("id")] long Id);
