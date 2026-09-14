using Defender.Portal.Application.Configuration.Extension;
using Defender.Portal.Application.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Defender.Portal.Tests.Services;

public sealed class CarServiceOptionsTests
{
    [Fact]
    public void AddApplicationOptions_WhenUrlHasMultipleTrailingSlashes_NormalizesToOneSlash()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(CarServiceOptions)}:Url"] = "https://car.test///",
            })
            .Build();
        using var provider = new ServiceCollection()
            .AddApplicationOptions(configuration)
            .BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<CarServiceOptions>>().Value;

        Assert.Equal("https://car.test/", options.Url);
    }

    [Fact]
    public void AddApplicationOptions_WhenTimeoutIsConfigured_BindsTimeoutSeconds()
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
            .BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<CarServiceOptions>>().Value;

        Assert.Equal(7, options.TimeoutSeconds);
    }

    [Fact]
    public void AddApplicationOptions_WhenTimeoutIsNotPositive_ThrowsOptionsValidationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(CarServiceOptions)}:Url"] = "https://car.test",
                [$"{nameof(CarServiceOptions)}:TimeoutSeconds"] = "0",
            })
            .Build();
        using var provider = new ServiceCollection()
            .AddApplicationOptions(configuration)
            .BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<CarServiceOptions>>().Value);
    }

    [Fact]
    public void AddApplicationOptions_WhenUrlIsNotAbsolute_ThrowsOptionsValidationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(CarServiceOptions)}:Url"] = "car.test",
            })
            .Build();
        using var provider = new ServiceCollection()
            .AddApplicationOptions(configuration)
            .BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<CarServiceOptions>>().Value);
    }
}
