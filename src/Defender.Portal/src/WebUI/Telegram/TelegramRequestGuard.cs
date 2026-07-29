using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Defender.Portal.WebUI.Telegram;

public static class TelegramRequestGuard
{
    private const string PortalOrigin = "https://portal.coded-by-danil.dev";

    public static bool BlocksMutation(HttpContext context)
    {
        if (!context.User.HasClaim("amr", "telegram") || HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method))
        {
            return false;
        }

        if (context.Request.Path.StartsWithSegments("/api/telegram"))
        {
            return !string.Equals(context.Request.Headers.Origin, PortalOrigin, StringComparison.Ordinal);
        }

        return true;
    }
}
