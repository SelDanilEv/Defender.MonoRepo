using Defender.Common.DTOs;
using Defender.Portal.Application.Common.Interfaces.Repositories;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.Accounts;
using Defender.Portal.Application.DTOs.Auth;

namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramSessionService(
    ITelegramInitDataValidator initDataValidator,
    ITelegramAccountLinkRepository accountLinkRepository,
    IIdentityWrapper identityWrapper,
    ITelegramSessionTokenIssuer tokenIssuer,
    TimeProvider timeProvider) : ITelegramSessionService
{
    public async Task<TelegramSessionResult> CreateSessionAsync(string initData, CancellationToken cancellationToken)
    {
        var telegramUser = initDataValidator.Validate(initData, timeProvider.GetUtcNow());
        var link = await accountLinkRepository.GetByTelegramUserIdAsync(telegramUser.TelegramUserId, cancellationToken)
            ?? throw new TelegramAccountNotLinkedException();
        var account = await identityWrapper.GetAccountDetailsAsServiceAsync(link.AccountId);
        if (account.IsBlocked || account.Roles.Count == 0)
        {
            throw new TelegramAccountNotLinkedException();
        }

        var token = tokenIssuer.Issue(account.Id, account.Roles);
        return new TelegramSessionResult(CreateSession(account), token);
    }

    public async Task LinkAsync(Guid accountId, string initData, CancellationToken cancellationToken)
    {
        var telegramUser = initDataValidator.Validate(initData, timeProvider.GetUtcNow());
        var accountLink = await accountLinkRepository.GetByAccountIdAsync(accountId, cancellationToken);
        if (accountLink != null)
        {
            if (accountLink.TelegramUserId == telegramUser.TelegramUserId)
            {
                return;
            }

            throw new TelegramAccountLinkConflictException();
        }

        var telegramLink = await accountLinkRepository.GetByTelegramUserIdAsync(telegramUser.TelegramUserId, cancellationToken);
        if (telegramLink != null)
        {
            throw new TelegramAccountLinkConflictException();
        }

        try
        {
            await accountLinkRepository.CreateAsync(
                new TelegramAccountLink(accountId, telegramUser.TelegramUserId, timeProvider.GetUtcNow()),
                cancellationToken);
        }
        catch (TelegramAccountLinkConflictException)
        {
            var persistedAccountLink = await accountLinkRepository.GetByAccountIdAsync(accountId, cancellationToken);
            if (persistedAccountLink?.TelegramUserId == telegramUser.TelegramUserId)
            {
                return;
            }

            throw;
        }
    }

    public Task UnlinkAsync(Guid accountId, CancellationToken cancellationToken) =>
        accountLinkRepository.DeleteByAccountIdAsync(accountId, cancellationToken);

    private static SessionDto CreateSession(AccountDto account) => new()
    {
        IsAuthenticated = true,
        User = new PortalAccountDto
        {
            Id = account.Id,
            IsBlocked = account.IsBlocked,
            IsEmailVerified = account.IsEmailVerified,
            IsPhoneVerified = account.IsPhoneVerified,
            Roles = account.Roles,
        },
    };
}
