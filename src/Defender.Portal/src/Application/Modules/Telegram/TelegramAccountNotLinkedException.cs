namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramAccountNotLinkedException : Exception
{
    public TelegramAccountNotLinkedException() : base("Telegram account is not linked.")
    {
    }
}
