using Defender.Portal.WebUI.Telegram;
using Microsoft.AspNetCore.Http;

namespace Defender.Portal.Tests.Services;

public class TelegramSessionCookieHelperTests
{
    [Fact]
    public void SetCookie_WhenTelegramMiniAppNeedsCrossSiteSession_AppendsSecureSameSiteNoneCookie()
    {
        var context = new DefaultHttpContext();

        TelegramSessionCookieHelper.SetCookie(context.Response, "telegram-token");

        var header = context.Response.Headers.SetCookie.ToString();
        Assert.Contains("__Host-Defender-Telegram=telegram-token", header, StringComparison.Ordinal);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", header, StringComparison.OrdinalIgnoreCase);
    }
}
