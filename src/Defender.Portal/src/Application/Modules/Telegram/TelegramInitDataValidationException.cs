namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramInitDataValidationException : Exception
{
    public TelegramInitDataValidationException() : base("Telegram init data is invalid.")
    {
    }
}
