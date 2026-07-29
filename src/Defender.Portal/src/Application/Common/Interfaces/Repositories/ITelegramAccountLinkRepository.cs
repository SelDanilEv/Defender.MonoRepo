using Defender.Portal.Application.Modules.Telegram;

namespace Defender.Portal.Application.Common.Interfaces.Repositories;

public interface ITelegramAccountLinkRepository
{
    Task<TelegramAccountLink?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken);

    Task<TelegramAccountLink?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken);

    Task CreateAsync(TelegramAccountLink link, CancellationToken cancellationToken);

    Task DeleteByAccountIdAsync(Guid accountId, CancellationToken cancellationToken);
}
