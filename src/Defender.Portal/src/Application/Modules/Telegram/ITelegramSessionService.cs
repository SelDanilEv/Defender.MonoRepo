namespace Defender.Portal.Application.Modules.Telegram;

public interface ITelegramSessionService
{
    Task<TelegramSessionResult> CreateSessionAsync(string initData, CancellationToken cancellationToken);

    Task LinkAsync(Guid accountId, string initData, CancellationToken cancellationToken);

    Task LinkTelegramUserAsync(Guid accountId, long telegramUserId, CancellationToken cancellationToken);

    Task UnlinkAsync(Guid accountId, CancellationToken cancellationToken);
}
