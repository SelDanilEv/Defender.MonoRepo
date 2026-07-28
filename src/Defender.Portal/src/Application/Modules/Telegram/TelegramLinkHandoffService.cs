using System.Security.Cryptography;
using Defender.Portal.Application.Common.Interfaces.Repositories;

namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramLinkHandoffService(
    ITelegramInitDataValidator initDataValidator,
    ITelegramLinkHandoffRepository handoffRepository,
    ITelegramSessionService sessionService,
    TimeProvider timeProvider) : ITelegramLinkHandoffService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    public async Task<TelegramLinkHandoff> CreateAsync(string initData, CancellationToken cancellationToken)
    {
        var telegramUser = initDataValidator.Validate(initData, timeProvider.GetUtcNow());
        var handoff = new TelegramLinkHandoff(
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
            telegramUser.TelegramUserId,
            timeProvider.GetUtcNow().Add(Lifetime));
        await handoffRepository.CreateAsync(HashCode(handoff.Code), handoff.TelegramUserId, handoff.ExpiresAt, cancellationToken);
        return handoff;
    }

    public async Task ConsumeAsync(Guid accountId, string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 64)
        {
            throw new TelegramLinkHandoffNotFoundException();
        }

        var telegramUserId = await handoffRepository.TryConsumeAsync(HashCode(code), timeProvider.GetUtcNow(), cancellationToken)
            ?? throw new TelegramLinkHandoffNotFoundException();
        await sessionService.LinkTelegramUserAsync(accountId, telegramUserId, cancellationToken);
    }

    private static string HashCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Convert.FromHexString(code))).ToLowerInvariant();
}
