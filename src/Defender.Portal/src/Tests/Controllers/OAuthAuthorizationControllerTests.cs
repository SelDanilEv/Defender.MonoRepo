using System.Reflection;
using Defender.Portal.WebUI.Controllers;
using Defender.Portal.WebUI.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Defender.Portal.Tests.Controllers;

public class OAuthAuthorizationControllerTests
{
    [Fact]
    public void RenderConsentPage_WhenAuthorizationQueryExists_IncludesHiddenAuthorizationParameters()
    {
        var controller = new OAuthAuthorizationController(Options.Create(new PortalOAuthOptions()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
        controller.HttpContext.Request.QueryString = new QueryString("?client_id=defender-mcp&redirect_uri=http%3A%2F%2F127.0.0.1%3A43123%2Fcallback&response_type=code");
        var renderConsentPage = typeof(OAuthAuthorizationController).GetMethod(
            "RenderConsentPage",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var html = Assert.IsType<string>(renderConsentPage.Invoke(controller, ["defender-mcp", new[] { "mcp:calendar:write" }]));

        Assert.Contains("<input type=\"hidden\" name=\"client_id\" value=\"defender-mcp\">", html);
        Assert.Contains("<input type=\"hidden\" name=\"redirect_uri\" value=\"http://127.0.0.1:43123/callback\">", html);
    }
}
