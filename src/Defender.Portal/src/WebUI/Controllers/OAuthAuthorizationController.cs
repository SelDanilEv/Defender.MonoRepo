using System.Security.Claims;
using System.Text.Encodings.Web;
using Defender.Portal.WebUI.OAuth;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Defender.Portal.WebUI.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class OAuthAuthorizationController(IOptions<PortalOAuthOptions> portalOAuthOptions) : Controller
{
    [HttpGet("~/oauth/authorize")]
    [HttpPost("~/oauth/authorize")]
    public IActionResult Authorize([FromForm] string? consent)
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return BadRequest();
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = $"{Request.PathBase}{Request.Path}{Request.QueryString}";
            return Redirect($"/welcome/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        if (string.Equals(consent, "deny", StringComparison.Ordinal))
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (!string.Equals(consent, "approve", StringComparison.Ordinal))
        {
            return Content(RenderConsentPage(request.ClientId, request.GetScopes()), "text/html");
        }

        var subject = User.FindFirstValue(Defender.Common.Consts.ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.SetClaim(OpenIddictConstants.Claims.Subject, subject);
        identity.SetClaim(OpenIddictConstants.Claims.Name, User.Identity.Name);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources(portalOAuthOptions.Value.McpAudience);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private string RenderConsentPage(string? clientId, IEnumerable<string> scopes)
    {
        var action = HtmlEncoder.Default.Encode($"{Request.PathBase}{Request.Path}{Request.QueryString}");
        var application = HtmlEncoder.Default.Encode(clientId ?? "MCP client");
        var requestedScopes = string.Join(
            string.Empty,
            scopes.Select(scope => $"<li>{HtmlEncoder.Default.Encode(scope)}</li>"));
        var authorizationParameters = string.Join(
            string.Empty,
            Request.Query.SelectMany(parameter => parameter.Value.Select(value =>
                $"<input type=\"hidden\" name=\"{HtmlEncoder.Default.Encode(parameter.Key)}\" value=\"{HtmlEncoder.Default.Encode(value ?? string.Empty)}\">")));

        return $"""
            <!doctype html>
            <html lang="en"><head><meta charset="utf-8"><title>Defender Portal access</title></head>
            <body><main><h1>Allow access to Defender Portal?</h1><p>{application} requests:</p>
            <ul>{requestedScopes}</ul><form method="post" action="{action}">
            {authorizationParameters}
            <button name="consent" value="approve" type="submit">Allow</button>
            <button name="consent" value="deny" type="submit">Deny</button></form></main></body></html>
            """;
    }
}
