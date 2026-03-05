using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Infrastructure;
using Defender.PersonalFoodAdviser.Infrastructure.Clients.Gemini;
using Defender.PersonalFoodAdviser.Infrastructure.Clients.HuggingFace;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.PersonalFoodAdviser.Tests.Infrastructure.Clients;

public class MenuIntelligenceRegistrationTests
{
    [Theory]
    [InlineData("HuggingFace", typeof(HuggingFaceClient))]
    [InlineData("Gemini", typeof(GeminiClient))]
    public void AddInfrastructureServices_WhenProviderConfigured_ResolvesConfiguredClientType(string provider, Type expectedType)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MenuIntelligenceOptions:Provider"] = provider,
                ["HuggingFaceOptions:BaseUrl"] = "https://api-inference.huggingface.co",
                ["GeminiOptions:BaseUrl"] = "https://generativelanguage.googleapis.com/v1beta"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IMenuIntelligenceClient>();

        Assert.IsType(expectedType, client);
    }
}
