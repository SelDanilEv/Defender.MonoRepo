using System.Collections.Immutable;
using Defender.Portal.WebUI.OAuth;

namespace Defender.Portal.Tests.Services;

public class PortalOAuthDiscoveryMetadataTests
{
    [Fact]
    public void Create_WhenPortalUsesPublicIssuer_PublishesDynamicClientRegistrationForPublicClients()
    {
        var metadata = PortalOAuthDiscoveryMetadata.Create(new PortalOAuthOptions
        {
            Issuer = "https://portal.coded-by-danil.dev/",
        });

        Assert.Equal("https://portal.coded-by-danil.dev/oauth/register", metadata[PortalOAuthDiscoveryMetadata.RegistrationEndpoint]);
        var methods = (ImmutableArray<string?>)metadata[PortalOAuthDiscoveryMetadata.TokenEndpointAuthMethodsSupported];
        Assert.Equal("none", methods.Single());
    }
}
