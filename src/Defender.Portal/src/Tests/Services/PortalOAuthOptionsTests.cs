using Defender.Portal.WebUI.OAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Defender.Portal.Tests.Services;

public class PortalOAuthOptionsTests
{
    [Fact]
    public void Validate_WhenMcpResourceUriIsHttps_ReturnsSuccess()
    {
        var options = new PortalOAuthOptions
        {
            Issuer = "https://portal.coded-by-danil.dev",
            McpResourceUri = "https://mcp.coded-by-danil.dev/mcp",
            McpAudience = "defender-mcp",
            BffAudience = "defender-api",
        };

        Assert.True(options.Validate().Succeeded);
    }

    [Fact]
    public void AddPortalOAuth_RegistersOAuthOptions()
    {
        var services = new ServiceCollection();
        services.AddPortalOAuth(BuildConfiguration());
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PortalOAuthOptions>>().Value;

        Assert.Equal("defender-mcp", options.McpAudience);
    }

    [Fact]
    public void AddPortalOAuth_RegistersOpenIddictApplicationManager()
    {
        var services = new ServiceCollection();

        services.AddPortalOAuth(BuildConfiguration());

        Assert.Contains(services, item => item.ServiceType == typeof(IOpenIddictApplicationManager));
    }

    [Fact]
    public void Configure_WhenDevelopmentSecretsAreMissing_UsesEphemeralCredentials()
    {
        var options = new OpenIddictServerOptions();
        var sut = new PortalOpenIddictServerOptions(
            new TestHostEnvironment(),
            Options.Create(new PortalOAuthOptions
            {
                Issuer = "https://portal.coded-by-danil.dev",
                McpResourceUri = "https://mcp.coded-by-danil.dev/mcp",
                McpAudience = "defender-mcp",
                BffAudience = "defender-api",
            }));

        sut.Configure(options);

        Assert.True(options.DisableAccessTokenEncryption);
        Assert.Single(options.SigningCredentials);
        Assert.Single(options.EncryptionCredentials);
    }

    private static IConfiguration BuildConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("PortalOAuth:Issuer", "https://portal.coded-by-danil.dev"),
            new KeyValuePair<string, string?>("PortalOAuth:McpResourceUri", "https://mcp.coded-by-danil.dev/mcp"),
            new KeyValuePair<string, string?>("PortalOAuth:McpAudience", "defender-mcp"),
            new KeyValuePair<string, string?>("PortalOAuth:BffAudience", "defender-api"),
        ])
        .Build();

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Defender.Portal.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
