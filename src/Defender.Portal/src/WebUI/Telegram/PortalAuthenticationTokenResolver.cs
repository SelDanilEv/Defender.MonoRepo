using Defender.Common.Helpers;
using Microsoft.AspNetCore.Http;

namespace Defender.Portal.WebUI.Telegram;

public static class PortalAuthenticationTokenResolver
{
    public static string? ResolveAndForward(HttpRequest request)
    {
        var token = AuthCookieHelper.GetAuthToken(request)
            ?? TelegramSessionCookieHelper.GetToken(request);

        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        request.Headers.Authorization = $"Bearer {token}";
        return token;
    }
}
