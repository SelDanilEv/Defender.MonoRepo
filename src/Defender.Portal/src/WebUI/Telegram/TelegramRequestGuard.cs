using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Defender.Portal.WebUI.Telegram;

public static class TelegramRequestGuard
{
    public static bool BlocksMutation(HttpContext context)
    {
        if (!context.User.HasClaim("amr", "telegram") || HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method))
        {
            return false;
        }

        return !context.Request.Path.StartsWithSegments("/api/telegram")
            && !context.Request.Path.StartsWithSegments("/api/integrations/telegram");
    }
}
