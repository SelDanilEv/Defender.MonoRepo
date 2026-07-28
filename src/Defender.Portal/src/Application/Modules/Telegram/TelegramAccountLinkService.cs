namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramAccountLinkService : ITelegramAccountLinkService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(15);
    private readonly Dictionary<Guid, TelegramAccountLink> _linksByAccountId = [];
    private readonly Dictionary<long, TelegramAccountLink> _linksByTelegramUserId = [];
    private readonly Lock _lock = new();

    public TelegramAccountLink Link(Guid accountId, long telegramUserId, DateTimeOffset linkedAt)
    {
        lock (_lock)
        {
            if (_linksByAccountId.TryGetValue(accountId, out var accountLink))
            {
                if (accountLink.TelegramUserId == telegramUserId)
                {
                    return accountLink;
                }

                throw new TelegramAccountLinkConflictException();
            }

            if (_linksByTelegramUserId.ContainsKey(telegramUserId))
            {
                throw new TelegramAccountLinkConflictException();
            }

            var link = new TelegramAccountLink(accountId, telegramUserId, linkedAt);
            _linksByAccountId.Add(accountId, link);
            _linksByTelegramUserId.Add(telegramUserId, link);
            return link;
        }
    }

    public TelegramAccountLink? GetByAccountId(Guid accountId)
    {
        lock (_lock)
        {
            return _linksByAccountId.GetValueOrDefault(accountId);
        }
    }

    public TelegramAccountLink? GetByTelegramUserId(long telegramUserId)
    {
        lock (_lock)
        {
            return _linksByTelegramUserId.GetValueOrDefault(telegramUserId);
        }
    }

    public TelegramSessionDto CreateSession(long telegramUserId, DateTimeOffset issuedAt)
    {
        var link = GetByTelegramUserId(telegramUserId)
            ?? throw new TelegramAccountNotLinkedException();

        return new TelegramSessionDto(
            link.AccountId,
            link.TelegramUserId,
            issuedAt,
            issuedAt.Add(SessionLifetime));
    }
}
