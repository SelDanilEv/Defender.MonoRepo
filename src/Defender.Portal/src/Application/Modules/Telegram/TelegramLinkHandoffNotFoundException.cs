namespace Defender.Portal.Application.Modules.Telegram;

public sealed class TelegramLinkHandoffNotFoundException() : Exception("Telegram link handoff is invalid or expired.");
