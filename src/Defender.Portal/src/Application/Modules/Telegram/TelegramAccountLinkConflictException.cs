namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramAccountLinkConflictException : Exception
{
    public TelegramAccountLinkConflictException() : base("Telegram account is already linked.")
    {
    }
}
