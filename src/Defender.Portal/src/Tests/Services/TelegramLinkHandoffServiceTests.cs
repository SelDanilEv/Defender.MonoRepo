using Defender.Portal.Application.Common.Interfaces.Repositories;
using Defender.Portal.Application.Modules.Telegram;

namespace Defender.Portal.Tests.Services;

public class TelegramLinkHandoffServiceTests
{
    [Fact]
    public async Task ConsumeAsync_WhenCodeIsUsedOnce_LinksValidatedTelegramUserToCurrentAccount()
    {
        var handoffs = new FakeHandoffRepository();
        var links = new StubLinkService();
        var service = new TelegramLinkHandoffService(new StubValidator(new TelegramInitData(123456789, "danil")), handoffs, links, new FixedTimeProvider());

        var created = await service.CreateAsync("signed-init-data", CancellationToken.None);
        Assert.NotEqual(created.Code, handoffs.LastStoredCodeHash);
        Assert.Equal(64, handoffs.LastStoredCodeHash.Length);
        await service.ConsumeAsync(Guid.Parse("6da6cfc5-6d8e-448b-9f4c-12d465ed1066"), created.Code, CancellationToken.None);

        Assert.Equal(123456789, links.LinkedTelegramUserId);
        await Assert.ThrowsAsync<TelegramLinkHandoffNotFoundException>(() =>
            service.ConsumeAsync(Guid.NewGuid(), created.Code, CancellationToken.None));
    }

    private sealed class StubValidator(TelegramInitData initData) : ITelegramInitDataValidator
    {
        public TelegramInitData Validate(string rawInitData, DateTimeOffset now) => initData;
    }

    private sealed class StubLinkService : ITelegramSessionService
    {
        public long? LinkedTelegramUserId { get; private set; }
        public Task<TelegramSessionResult> CreateSessionAsync(string initData, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task LinkAsync(Guid accountId, string initData, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task LinkTelegramUserAsync(Guid accountId, long telegramUserId, CancellationToken cancellationToken) { LinkedTelegramUserId = telegramUserId; return Task.CompletedTask; }
        public Task UnlinkAsync(Guid accountId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeHandoffRepository : ITelegramLinkHandoffRepository
    {
        private readonly Dictionary<string, (long TelegramUserId, DateTimeOffset ExpiresAt)> handoffs = [];
        public string LastStoredCodeHash { get; private set; } = string.Empty;
        public Task CreateAsync(string codeHash, long telegramUserId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        {
            LastStoredCodeHash = codeHash;
            handoffs.Add(codeHash, (telegramUserId, expiresAt));
            return Task.CompletedTask;
        }
        public Task<long?> TryConsumeAsync(string codeHash, DateTimeOffset now, CancellationToken cancellationToken)
        {
            if (!handoffs.Remove(codeHash, out var handoff) || handoff.ExpiresAt <= now) return Task.FromResult<long?>(null);
            return Task.FromResult<long?>(handoff.TelegramUserId);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 7, 28, 18, 30, 0, TimeSpan.Zero);
    }
}
