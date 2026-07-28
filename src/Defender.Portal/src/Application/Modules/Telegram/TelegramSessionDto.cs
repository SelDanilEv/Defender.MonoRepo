namespace Defender.Portal.Application.Modules.Telegram;

public sealed record TelegramSessionDto(
    Guid AccountId,
    long TelegramUserId,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
