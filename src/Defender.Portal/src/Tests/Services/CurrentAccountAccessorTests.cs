using Defender.Common.Accessors;
using Defender.Common.Consts;
using Microsoft.AspNetCore.Http;

namespace Defender.Portal.Tests.Services;

public class CurrentAccountAccessorTests
{
    [Fact]
    public void Token_WhenAuthorizationHeaderExists_ReturnsHeaderValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer header-token";
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var sut = new CurrentAccountAccessor(httpContextAccessor);

        var result = sut.Token;

        Assert.Equal("Bearer header-token", result);
    }

    [Fact]
    public void Token_WhenHeaderMissingAndCookieExists_ReturnsCookieAsBearer()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{CookieNames.Authentication}=cookie-token";
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var sut = new CurrentAccountAccessor(httpContextAccessor);

        var result = sut.Token;

        Assert.Equal("Bearer cookie-token", result);
    }

    [Fact]
    public void Token_WhenHeaderAndCookieMissing_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var sut = new CurrentAccountAccessor(httpContextAccessor);

        var result = sut.Token;

        Assert.Null(result);
    }
}
