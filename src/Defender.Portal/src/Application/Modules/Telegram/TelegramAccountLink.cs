namespace Defender.Portal.Application.Modules.Telegram;

public sealed record TelegramAccountLink(Guid AccountId, long TelegramUserId, DateTimeOffset LinkedAt);
