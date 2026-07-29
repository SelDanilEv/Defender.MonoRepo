using Defender.Portal.Application.DTOs.Auth;

namespace Defender.Portal.Application.Modules.Telegram;

public sealed record TelegramSessionResult(SessionDto Session, string CookieToken);
