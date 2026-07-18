using System.Security.Cryptography;
using Defender.Common.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Defender.Portal.WebUI.OAuth;

public sealed class PortalOpenIddictServerOptions(
    IHostEnvironment hostEnvironment,
    IOptions<PortalOAuthOptions> portalOAuthOptions)
    : IConfigureOptions<OpenIddictServerOptions>
{
    public void Configure(OpenIddictServerOptions options)
    {
        var portalOptions = portalOAuthOptions.Value;
        var signingKey = GetRsaKey(portalOptions.SigningKeySecretName);
        var encryptionKey = GetRsaKey(portalOptions.EncryptionKeySecretName);

        options.Issuer = new Uri(portalOptions.Issuer);
        options.AuthorizationEndpointUris.Add(new Uri("/oauth/authorize", UriKind.Relative));
        options.TokenEndpointUris.Add(new Uri("/oauth/token", UriKind.Relative));
        options.IntrospectionEndpointUris.Add(new Uri("/oauth/introspect", UriKind.Relative));
        options.UserInfoEndpointUris.Add(new Uri("/oauth/userinfo", UriKind.Relative));
        options.GrantTypes.Add(OpenIddictConstants.GrantTypes.AuthorizationCode);
        options.GrantTypes.Add(OpenIddictConstants.GrantTypes.RefreshToken);
        options.ResponseTypes.Add(OpenIddictConstants.ResponseTypes.Code);
        options.RequireProofKeyForCodeExchange = true;
        options.Scopes.Add(PortalOAuthScopes.Read);
        options.Scopes.Add(PortalOAuthScopes.CalendarWrite);
        options.Scopes.Add(PortalOAuthScopes.CalendarDelete);
        options.DisableAccessTokenEncryption = true;
        options.EncryptionCredentials.Add(
            new EncryptingCredentials(
                new RsaSecurityKey(encryptionKey),
                SecurityAlgorithms.RsaOAEP,
                SecurityAlgorithms.Aes256CbcHmacSha512));
        options.SigningCredentials.Add(
            new SigningCredentials(new RsaSecurityKey(signingKey), SecurityAlgorithms.RsaSha256));
    }

    private RSA GetRsaKey(string secretName)
    {
        var key = RSA.Create();
        var pem = SecretsHelper.GetSecretSync(secretName);

        if (string.IsNullOrWhiteSpace(pem) && hostEnvironment.IsDevelopment())
        {
            return key;
        }

        key.ImportFromPem(pem);
        return key;
    }
}
