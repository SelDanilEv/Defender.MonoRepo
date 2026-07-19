using System.Net;
using System.Text.Json.Serialization;
using Defender.Portal.WebUI.OAuth;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace Defender.Portal.WebUI.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[EnableRateLimiting("oauth-registration")]
public sealed class OAuthDynamicClientRegistrationController(
    IOpenIddictApplicationManager applicationManager) : ControllerBase
{
    [HttpPost("~/oauth/register")]
    public async Task<ActionResult<DynamicClientRegistrationResponse>> RegisterAsync(
        [FromBody] DynamicClientRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.RedirectUris is null || request.RedirectUris.Length == 0 || request.RedirectUris.Any(uri => !IsAllowedRedirectUri(uri)))
        {
            return BadRequest(new { error = "invalid_redirect_uri" });
        }

        var clientId = $"mcp-{Guid.NewGuid():N}";
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
            DisplayName = string.IsNullOrWhiteSpace(request.ClientName) ? "MCP client" : request.ClientName.Trim(),
        };

        foreach (var uri in request.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri));
        }

        descriptor.Permissions.UnionWith(
        [
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            OpenIddictConstants.Permissions.ResponseTypes.Code,
            .. PortalOAuthScopes.All.Select(scope => OpenIddictConstants.Permissions.Prefixes.Scope + scope),
        ]);

        await applicationManager.CreateAsync(descriptor, cancellationToken);
        return Created(string.Empty, new DynamicClientRegistrationResponse(clientId, "none", request.RedirectUris));
    }

    private static bool IsAllowedRedirectUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                    || (IPAddress.TryParse(uri.Host, out var address) && IPAddress.IsLoopback(address))));
    }
}

public sealed record DynamicClientRegistrationRequest(
    [property: JsonPropertyName("redirect_uris")] string[]? RedirectUris,
    [property: JsonPropertyName("client_name")] string? ClientName);

public sealed record DynamicClientRegistrationResponse(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("token_endpoint_auth_method")] string TokenEndpointAuthMethod,
    [property: JsonPropertyName("redirect_uris")] string[] RedirectUris);
