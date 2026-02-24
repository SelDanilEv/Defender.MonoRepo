using Defender.UserManagementService.Application;
using Defender.UserManagementService.Application.Common.Interfaces.Services;
using Defender.UserManagementService.Application.Configuration.Extension;
using Defender.UserManagementService.Application.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Defender.UserManagementService.Tests.Configuration;

public class ApplicationConfigurationTests
{
    [Fact]
    public void AddApplicationOptions_WhenConfigurationProvided_BindsIdentityOptions()
    {
        var values = new Dictionary<string, string?>
        {
            [$"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Url)}"] = "http://identity.local"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();

        services.AddApplicationOptions(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.Equal("http://identity.local", options.Url);
    }

    [Fact]
    public void AddApplicationServices_WhenCalled_RegistersApplicationServices()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddApplicationServices(configuration);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IUserManagementService) &&
            d.ImplementationType == typeof(Defender.UserManagementService.Application.Services.UserManagementService));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(IAccessCodeService) &&
            d.ImplementationType == typeof(Defender.UserManagementService.Application.Services.AccessCodeService));
    }

    [Fact]
    public void IdentityOptions_WhenCreated_HasEmptyUrlByDefault()
    {
        var options = new IdentityOptions();

        Assert.Equal(string.Empty, options.Url);
    }
}
