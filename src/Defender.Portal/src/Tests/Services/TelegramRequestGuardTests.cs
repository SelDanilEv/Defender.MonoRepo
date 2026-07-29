using System.Security.Claims;
using Defender.Portal.WebUI.Telegram;
using Microsoft.AspNetCore.Http;

namespace Defender.Portal.Tests.Services;

public class TelegramRequestGuardTests
{
    [Fact]
    public void BlocksMutation_WhenTelegramSessionPostsToPortalApi_ReturnsTrue()
    {
        var context = CreateTelegramContext("POST", "/api/Banking/transfer");

        Assert.True(TelegramRequestGuard.BlocksMutation(context));
    }

    [Fact]
    public void BlocksMutation_WhenTelegramSessionReadsPortalApi_ReturnsFalse()
    {
        var context = CreateTelegramContext("GET", "/api/Wallet/info");

        Assert.False(TelegramRequestGuard.BlocksMutation(context));
    }

    [Fact]
    public void BlocksMutation_WhenTelegramSessionPostsToTelegramApiFromForeignOrigin_ReturnsTrue()
    {
        var context = CreateTelegramContext("DELETE", "/api/telegram/link");
        context.Request.Headers.Origin = "https://attacker.example";

        Assert.True(TelegramRequestGuard.BlocksMutation(context));
    }

    [Fact]
    public void BlocksMutation_WhenTelegramSessionPostsToTelegramApiFromPortalOrigin_ReturnsFalse()
    {
        var context = CreateTelegramContext("DELETE", "/api/telegram/link");
        context.Request.Headers.Origin = "https://portal.coded-by-danil.dev";

        Assert.False(TelegramRequestGuard.BlocksMutation(context));
    }

    private static HttpContext CreateTelegramContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("amr", "telegram")], "Bearer"));
        return context;
    }
}
