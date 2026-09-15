using Defender.Portal.Application.Configuration.Extension;
using Defender.Portal.Application.Configuration.Options;
using Defender.Portal.Infrastructure;
using Defender.Portal.Infrastructure.Clients.CarService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.Portal.Tests.Infrastructure.Clients;

public sealed class CarServiceRegistrationTests
{
    [Fact]
    public void AddInfrastructureServices_WhenTimeoutIsConfigured_UsesItForCarServiceHttpClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(CarServiceOptions)}:Url"] = "https://car.test",
                [$"{nameof(CarServiceOptions)}:TimeoutSeconds"] = "7",
            })
            .Build();
        using var provider = new ServiceCollection()
            .AddApplicationOptions(configuration)
            .AddInfrastructureServices()
            .BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(nameof(ICarServiceClient));

        Assert.Equal(TimeSpan.FromSeconds(7), client.Timeout);
        Assert.Equal("https://car.test/", client.BaseAddress!.ToString());
    }
}
