namespace Defender.Portal.Application.Modules.Telegram;

public interface ITelegramAccountLinkService
{
    TelegramAccountLink Link(Guid accountId, long telegramUserId, DateTimeOffset linkedAt);

    TelegramAccountLink? GetByAccountId(Guid accountId);

    TelegramAccountLink? GetByTelegramUserId(long telegramUserId);

    TelegramSessionDto CreateSession(long telegramUserId, DateTimeOffset issuedAt);
}
