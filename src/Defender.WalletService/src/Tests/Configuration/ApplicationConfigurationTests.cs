using Defender.WalletService.Application;
using Defender.WalletService.Application.Common.Interfaces.Services;
using Defender.WalletService.Application.Configuration.Extension;
using Defender.WalletService.Application.Configuration.Options;
using Defender.WalletService.Application.Services.Background.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Defender.WalletService.Tests.Configuration;

public class ApplicationConfigurationTests
{
    private const string SecretPrefix = "Defender_App_";

    [Fact]
    public void AddApplicationOptions_WhenConfigurationProvided_BindsServiceOptions()
    {
        var values = new Dictionary<string, string?>
        {
            [$"{nameof(ServiceOptions)}:{nameof(ServiceOptions.Url)}"] = "http://wallet.local"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();

        services.AddApplicationOptions(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ServiceOptions>>().Value;
        Assert.Equal("http://wallet.local", options.Url);
    }

    [Fact]
    public void AddApplicationServices_WhenCalled_RegistersCoreApplicationServices()
    {
        var distributedCacheConnection = "Host=localhost;Port=5432;Database=test;Username=test;Password=test";
        var envKey = SecretPrefix + "DistributedCacheConnectionString";
        Environment.SetEnvironmentVariable(envKey, distributedCacheConnection, EnvironmentVariableTarget.Process);

        try
        {
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();

            services.AddApplicationServices(configuration);

            Assert.Contains(services, d =>
                d.ServiceType == typeof(IWalletManagementService) &&
                d.ImplementationType == typeof(Defender.WalletService.Application.Services.WalletManagementService));
            Assert.Contains(services, d =>
                d.ServiceType == typeof(ITransactionManagementService) &&
                d.ImplementationType == typeof(Defender.WalletService.Application.Services.TransactionManagementService));
            Assert.Contains(services, d =>
                d.ServiceType == typeof(ITransactionProcessingService) &&
                d.ImplementationType == typeof(Defender.WalletService.Application.Services.TransactionProcessingService));
            Assert.Contains(services, d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(CreateKafkaTopicsService));
            Assert.Contains(services, d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(EventListenerService));
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    public void ServiceOptions_WhenCreated_HasEmptyUrlByDefault()
    {
        var options = new ServiceOptions();

        Assert.Equal(string.Empty, options.Url);
    }
}
