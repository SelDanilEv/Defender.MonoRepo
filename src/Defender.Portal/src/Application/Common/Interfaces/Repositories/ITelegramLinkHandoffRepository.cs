using Defender.Portal.Application.Modules.Telegram;

namespace Defender.Portal.Application.Common.Interfaces.Repositories;

public interface ITelegramLinkHandoffRepository
{
    Task CreateAsync(string codeHash, long telegramUserId, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task<long?> TryConsumeAsync(string codeHash, DateTimeOffset now, CancellationToken cancellationToken);
}
