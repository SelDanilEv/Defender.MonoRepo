using Defender.Common.Consts;
using Defender.Common.Helpers;
using Microsoft.AspNetCore.Http;

namespace Defender.Portal.Tests.Services;

public class AuthCookieHelperTests
{
    [Fact]
    public void SetAuthCookie_WhenTokenProvided_AppendsSecureHttpOnlyCookie()
    {
        var context = new DefaultHttpContext();

        AuthCookieHelper.SetAuthCookie(context.Response, "token-value");

        var header = context.Response.Headers.SetCookie.ToString();
        Assert.Contains($"{CookieNames.Authentication}=token-value", header, StringComparison.Ordinal);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetAuthCookie_WhenTokenMissing_DoesNotAppendCookie()
    {
        var context = new DefaultHttpContext();

        AuthCookieHelper.SetAuthCookie(context.Response, string.Empty);

        Assert.True(string.IsNullOrWhiteSpace(context.Response.Headers.SetCookie.ToString()));
    }

    [Fact]
    public void ClearAuthCookie_WhenCalled_AppendsCookieRemovalHeader()
    {
        var context = new DefaultHttpContext();

        AuthCookieHelper.ClearAuthCookie(context.Response);

        var header = context.Response.Headers.SetCookie.ToString();
        Assert.Contains($"{CookieNames.Authentication}=", header, StringComparison.Ordinal);
        Assert.Contains("expires=", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetAuthToken_WhenCookiePresent_ReturnsCookieToken()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{CookieNames.Authentication}=token-value";

        var result = AuthCookieHelper.GetAuthToken(context.Request);

        Assert.Equal("token-value", result);
    }
}
