namespace Defender.Portal.Application.Modules.Telegram;

public interface ITelegramSessionTokenIssuer
{
    string Issue(Guid accountId, IReadOnlyCollection<string> roles);
}
