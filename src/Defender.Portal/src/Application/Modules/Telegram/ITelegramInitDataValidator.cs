namespace Defender.Portal.Application.Modules.Telegram;

public interface ITelegramInitDataValidator
{
    TelegramInitData Validate(string rawInitData, DateTimeOffset now);
}
