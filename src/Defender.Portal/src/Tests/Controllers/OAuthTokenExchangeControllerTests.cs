using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Defender.Portal.WebUI.Controllers;
using Defender.Portal.WebUI.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using CustomClaimTypes = Defender.Common.Consts.ClaimTypes;
using Roles = Defender.Common.Consts.Roles;

namespace Defender.Portal.Tests.Controllers;

public class OAuthTokenExchangeControllerTests
{
    [Fact]
    public void Exchange_WhenMcpSubjectIsPresent_IssuesBffTokenWithUserRole()
    {
        const string secretKey = "Defender_App_JwtSecret";
        var originalSecret = Environment.GetEnvironmentVariable(secretKey, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(secretKey, "test-jwt-secret-with-at-least-32-bytes", EnvironmentVariableTarget.Process);

        try
        {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("JwtTokenIssuer", "Defender"),
            ])
            .Build();
        var controller = new OAuthTokenExchangeController(
            configuration,
            Options.Create(new PortalOAuthOptions { BffAudience = "defender-api" }))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                        [
                            new Claim(OpenIddictConstants.Claims.Subject, Guid.NewGuid().ToString()),
                        ],
                        "Bearer")),
                },
            },
        };

        var actionResult = controller.Exchange();
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<TokenExchangeResponse>(result.Value);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        Assert.Contains(token.Claims, claim => claim.Type == CustomClaimTypes.Role && claim.Value == Roles.User);
        }
        finally
        {
            Environment.SetEnvironmentVariable(secretKey, originalSecret, EnvironmentVariableTarget.Process);
        }
    }
}
