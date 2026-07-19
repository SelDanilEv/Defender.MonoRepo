using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Defender.Common.Enums;
using Defender.Common.Helpers;
using Defender.Portal.WebUI.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace Defender.Portal.WebUI.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public sealed class OAuthTokenExchangeController(
    IConfiguration configuration,
    IOptions<PortalOAuthOptions> portalOAuthOptions) : ControllerBase
{
    [HttpPost("~/oauth/token-exchange")]
    public ActionResult<TokenExchangeResponse> Exchange()
    {
        var subject = User.FindFirstValue(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Forbid();
        }

        var expires = DateTime.UtcNow.AddSeconds(60);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(Defender.Common.Consts.ClaimTypes.NameIdentifier, subject),
                new Claim(Defender.Common.Consts.ClaimTypes.Role, Defender.Common.Consts.Roles.User),
            ]),
            Expires = expires,
            Issuer = configuration["JwtTokenIssuer"],
            Audience = portalOAuthOptions.Value.BffAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretsHelper.GetSecretSync(Secret.JwtSecret, true))),
                SecurityAlgorithms.HmacSha256),
        };

        var token = new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
        return Ok(new TokenExchangeResponse(token, "Bearer", 60));
    }
}

public sealed record TokenExchangeResponse(string AccessToken, string TokenType, int ExpiresIn);
