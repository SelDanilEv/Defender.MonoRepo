using Microsoft.AspNetCore.Http;

namespace Defender.Portal.WebUI.Telegram;

public static class TelegramSessionCookieHelper
{
    public const string CookieName = "__Host-Defender-Telegram";

    public static void SetCookie(HttpResponse response, string token) =>
        response.Cookies.Append(CookieName, token, CookieOptions());

    public static void ClearCookie(HttpResponse response) =>
        response.Cookies.Delete(CookieName, CookieOptions());

    public static string? GetToken(HttpRequest request) =>
        request.Cookies.TryGetValue(CookieName, out var token) && !string.IsNullOrWhiteSpace(token)
            ? token
            : null;

    private static CookieOptions CookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        IsEssential = true,
    };
}
