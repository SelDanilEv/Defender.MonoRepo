using Defender.Portal.Application.Modules.Telegram;

namespace Defender.Portal.Tests.Services;

public class TelegramAccountLinkServiceTests
{
    private static readonly DateTimeOffset LinkedAt = new(2026, 7, 28, 18, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Link_WhenAccountAndTelegramUserAreUnlinked_CreatesOneToOneLink()
    {
        var sut = new TelegramAccountLinkService();
        var accountId = Guid.Parse("6da6cfc5-6d8e-448b-9f4c-12d465ed1066");

        var result = sut.Link(accountId, 123456789, LinkedAt);

        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(123456789, result.TelegramUserId);
        Assert.Equal(LinkedAt, result.LinkedAt);
        Assert.Equal(result, sut.GetByAccountId(accountId));
        Assert.Equal(result, sut.GetByTelegramUserId(123456789));
    }

    [Fact]
    public void Link_WhenTelegramUserIsLinkedToAnotherAccount_ThrowsTelegramAccountLinkConflictException()
    {
        var sut = new TelegramAccountLinkService();
        sut.Link(Guid.Parse("6da6cfc5-6d8e-448b-9f4c-12d465ed1066"), 123456789, LinkedAt);

        Assert.Throws<TelegramAccountLinkConflictException>(() =>
            sut.Link(Guid.Parse("a5c79fe8-c8b4-4a0c-8d96-8d55e6b0338a"), 123456789, LinkedAt));
    }

    [Fact]
    public void Link_WhenAccountIsLinkedToAnotherTelegramUser_ThrowsTelegramAccountLinkConflictException()
    {
        var sut = new TelegramAccountLinkService();
        var accountId = Guid.Parse("6da6cfc5-6d8e-448b-9f4c-12d465ed1066");
        sut.Link(accountId, 123456789, LinkedAt);

        Assert.Throws<TelegramAccountLinkConflictException>(() => sut.Link(accountId, 987654321, LinkedAt));
    }

    [Fact]
    public void Link_WhenSameAccountAndTelegramUserAreLinked_ReturnsExistingLink()
    {
        var sut = new TelegramAccountLinkService();
        var accountId = Guid.Parse("6da6cfc5-6d8e-448b-9f4c-12d465ed1066");
        var expected = sut.Link(accountId, 123456789, LinkedAt);

        var result = sut.Link(accountId, 123456789, LinkedAt.AddMinutes(1));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CreateSession_WhenTelegramUserIsLinked_ReturnsSessionExpiringInFifteenMinutes()
    {
        var sut = new TelegramAccountLinkService();
        var accountId = Guid.Parse("6da6cfc5-6d8e-448b-9f4c-12d465ed1066");
        sut.Link(accountId, 123456789, LinkedAt);

        var result = sut.CreateSession(123456789, LinkedAt.AddMinutes(5));

        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(123456789, result.TelegramUserId);
        Assert.Equal(LinkedAt.AddMinutes(5), result.IssuedAt);
        Assert.Equal(LinkedAt.AddMinutes(20), result.ExpiresAt);
    }

    [Fact]
    public void CreateSession_WhenTelegramUserIsNotLinked_ThrowsTelegramAccountNotLinkedException()
    {
        var sut = new TelegramAccountLinkService();

        Assert.Throws<TelegramAccountNotLinkedException>(() => sut.CreateSession(123456789, LinkedAt));
    }
}
