using Defender.Common.DTOs;
using Defender.Portal.Application.Common.Interfaces.Repositories;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.Modules.Telegram;

namespace Defender.Portal.Tests.Services;

public class TelegramSessionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 18, 30, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.Parse("6da6cfc5-6d8e-448b-9f4c-12d465ed1066");

    [Fact]
    public async Task CreateSessionAsync_WhenTelegramAccountIsLinked_ReturnsCookieOnlyPortalSession()
    {
        var repository = new FakeTelegramAccountLinkRepository();
        await repository.CreateAsync(new TelegramAccountLink(AccountId, 123456789, Now), CancellationToken.None);
        var identity = new Mock<IIdentityWrapper>();
        identity.Setup(value => value.GetAccountDetailsAsServiceAsync(AccountId)).ReturnsAsync(new AccountDto
        {
            Id = AccountId,
            Roles = ["User", "Guest"],
            IsEmailVerified = true,
        });
        var issuer = new Mock<ITelegramSessionTokenIssuer>();
        issuer.Setup(value => value.Issue(AccountId, It.IsAny<IReadOnlyCollection<string>>())).Returns("signed-token");
        var service = new TelegramSessionService(
            new StubInitDataValidator(new TelegramInitData(123456789, "danil")),
            repository,
            identity.Object,
            issuer.Object,
            new FixedTimeProvider(Now));

        var result = await service.CreateSessionAsync("signed-init-data", CancellationToken.None);

        Assert.Equal("signed-token", result.CookieToken);
        Assert.True(result.Session.IsAuthenticated);
        Assert.Null(result.Session.Token);
        Assert.NotNull(result.Session.User);
        Assert.Equal(AccountId, result.Session.User.Id);
        Assert.Contains("User", result.Session.User.Roles);
    }

    [Fact]
    public async Task LinkAsync_WhenTelegramUserBelongsToAnotherAccount_ThrowsTelegramAccountLinkConflictException()
    {
        var repository = new FakeTelegramAccountLinkRepository();
        await repository.CreateAsync(new TelegramAccountLink(Guid.Parse("a5c79fe8-c8b4-4a0c-8d96-8d55e6b0338a"), 123456789, Now), CancellationToken.None);
        var service = CreateService(repository, new TelegramInitData(123456789, "danil"));

        await Assert.ThrowsAsync<TelegramAccountLinkConflictException>(() =>
            service.LinkAsync(AccountId, "signed-init-data", CancellationToken.None));
    }

    [Fact]
    public async Task UnlinkAsync_WhenAccountHasTelegramLink_RemovesPersistentLink()
    {
        var repository = new FakeTelegramAccountLinkRepository();
        await repository.CreateAsync(new TelegramAccountLink(AccountId, 123456789, Now), CancellationToken.None);
        var service = CreateService(repository, new TelegramInitData(123456789, "danil"));

        await service.UnlinkAsync(AccountId, CancellationToken.None);

        Assert.Null(await repository.GetByAccountIdAsync(AccountId, CancellationToken.None));
    }

    private static TelegramSessionService CreateService(
        ITelegramAccountLinkRepository repository,
        TelegramInitData initData) => new(
        new StubInitDataValidator(initData),
        repository,
        Mock.Of<IIdentityWrapper>(),
        Mock.Of<ITelegramSessionTokenIssuer>(),
        new FixedTimeProvider(Now));

    private sealed class StubInitDataValidator(TelegramInitData initData) : ITelegramInitDataValidator
    {
        public TelegramInitData Validate(string rawInitData, DateTimeOffset now) => initData;
    }

    private sealed class FakeTelegramAccountLinkRepository : ITelegramAccountLinkRepository
    {
        private readonly List<TelegramAccountLink> links = [];

        public Task<TelegramAccountLink?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult<TelegramAccountLink?>(links.SingleOrDefault(link => link.AccountId == accountId));

        public Task<TelegramAccountLink?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken) =>
            Task.FromResult<TelegramAccountLink?>(links.SingleOrDefault(link => link.TelegramUserId == telegramUserId));

        public Task CreateAsync(TelegramAccountLink link, CancellationToken cancellationToken)
        {
            links.Add(link);
            return Task.CompletedTask;
        }

        public Task DeleteByAccountIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            links.RemoveAll(link => link.AccountId == accountId);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
