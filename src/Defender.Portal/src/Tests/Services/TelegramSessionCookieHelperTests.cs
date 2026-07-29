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

    [Fact]
    public void ResolveAndForward_WhenTelegramSessionCookieIsPresent_ForwardsBearerTokenToDownstreamServices()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "__Host-Defender-Telegram=telegram-token";

        var token = PortalAuthenticationTokenResolver.ResolveAndForward(context.Request);

        Assert.Equal("telegram-token", token);
        Assert.Equal("Bearer telegram-token", context.Request.Headers.Authorization.ToString());
    }
}
