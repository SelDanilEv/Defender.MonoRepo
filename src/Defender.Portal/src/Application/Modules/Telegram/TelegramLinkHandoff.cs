namespace Defender.Portal.Application.Modules.Telegram;

public sealed record TelegramLinkHandoff(string Code, long TelegramUserId, DateTimeOffset ExpiresAt);
