using System.Collections.Immutable;
using OpenIddict.Abstractions;

namespace Defender.Portal.WebUI.OAuth;

public static class PortalOAuthDiscoveryMetadata
{
    public const string RegistrationEndpoint = "registration_endpoint";
    public const string TokenEndpointAuthMethodsSupported = "token_endpoint_auth_methods_supported";

    public static IReadOnlyDictionary<string, OpenIddictParameter> Create(PortalOAuthOptions options)
    {
        var issuer = options.Issuer.TrimEnd('/');
        return new Dictionary<string, OpenIddictParameter>
        {
            [RegistrationEndpoint] = $"{issuer}/oauth/register",
            [TokenEndpointAuthMethodsSupported] = ImmutableArray.Create<string?>("none"),
        };
    }
}
