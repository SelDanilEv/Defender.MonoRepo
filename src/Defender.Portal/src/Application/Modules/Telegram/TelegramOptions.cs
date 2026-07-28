namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; init; } = string.Empty;

    public TimeSpan InitDataMaximumAge { get; init; } = TimeSpan.FromMinutes(5);
}
