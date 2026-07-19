using Defender.Portal.WebUI.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Defender.Portal.WebUI.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class OAuthAuthorizationServerMetadataController(IOptions<PortalOAuthOptions> portalOAuthOptions) : ControllerBase
{
    [HttpGet("~/.well-known/oauth-authorization-server")]
    public ActionResult Get()
    {
        var issuer = portalOAuthOptions.Value.Issuer.TrimEnd('/');
        return Ok(new
        {
            issuer,
            authorization_endpoint = $"{issuer}/oauth/authorize",
            token_endpoint = $"{issuer}/oauth/token",
            jwks_uri = $"{issuer}/.well-known/jwks",
            registration_endpoint = $"{issuer}/oauth/register",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "refresh_token" },
            code_challenge_methods_supported = new[] { "S256" },
            scopes_supported = PortalOAuthScopes.All,
        });
    }
}
