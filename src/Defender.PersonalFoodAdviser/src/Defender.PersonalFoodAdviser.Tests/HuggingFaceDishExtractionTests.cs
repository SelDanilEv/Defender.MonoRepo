using Defender.PersonalFoodAdviser.Application.Configuration.Options;
using Defender.PersonalFoodAdviser.Infrastructure.Clients.HuggingFace;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Defender.PersonalFoodAdviser.Tests;

public class HuggingFaceDishExtractionTests
{
    [Fact]
    public async Task ExtractDishNamesFromImages_WhenApiReturnsArrayWithGeneratedText_ParsesDishNames()
    {
        var json = """[{"generated_text": "Caesar Salad\nGrilled Salmon\nGarlic Bread"}]""";
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01, 0x02 }]);

        Assert.Equal(3, result.Count);
        Assert.Contains("Caesar Salad", result);
        Assert.Contains("Grilled Salmon", result);
        Assert.Contains("Garlic Bread", result);
    }

    [Fact]
    public async Task ExtractDishNamesFromImages_WhenApiReturnsSingleObjectWithGeneratedText_ParsesDishNames()
    {
        var json = """{"generated_text": "Pasta Carbonara\nTiramisu"}""";
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        Assert.Equal(2, result.Count);
        Assert.Contains("Pasta Carbonara", result);
        Assert.Contains("Tiramisu", result);
    }

    [Fact]
    public async Task ExtractDishNamesFromImages_WhenApiReturnsSummaryText_InArray_ParsesDishNames()
    {
        var json = """[{"summary_text": "Burger\nFries\nCola"}]""";
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        Assert.Equal(3, result.Count);
        Assert.Contains("Burger", result);
        Assert.Contains("Fries", result);
        Assert.Contains("Cola", result);
    }

    [Fact]
    public async Task ExtractDishNamesFromImages_WhenApiReturnsEmptyArray_ReturnsEmptyList()
    {
        var json = "[]";
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractDishNamesFromImages_WhenApiReturnsObjectWithoutGeneratedTextOrSummaryText_ReturnsEmptyList()
    {
        var json = """{"error": "Model loading", "estimated_time": 30}""";
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractDishNamesFromImages_WhenApiKeyNotConfigured_ReturnsEmptyList()
    {
        var client = CreateClientWithResponse("[]", apiKey: "");

        var result = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractDishNamesFromImages_StripsPricesAndNumbers_FromDishLines()
    {
        var json = """[{"generated_text": "1. Steak 25.00\n2. Soup 8.50\n- Dessert 12"}]""";
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        Assert.Contains("Steak", result);
        Assert.Contains("Soup", result);
        Assert.Contains("Dessert", result);
        Assert.DoesNotContain(result, s => s.Contains("25") || s.Contains("8.50") || s.Contains("12"));
    }

    [Fact]
    public async Task ExtractDishNamesFromImages_WhenModelReturnsCommaOrSemicolonSeparated_ParsesAll()
    {
        var json = """[{"generated_text": "Pizza, Pasta; Salad\nBurger"}]""";
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        Assert.True(result.Count >= 3);
        Assert.Contains("Pizza", result);
        Assert.Contains("Pasta", result);
        Assert.Contains("Salad", result);
        Assert.Contains("Burger", result);
    }

    private static HuggingFaceClient CreateClientWithResponse(string responseJson, string apiKey)
    {
        var handler = new MockHttpMessageHandler(responseJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api-inference.huggingface.co/") };
        var options = Options.Create(new HuggingFaceOptions
        {
            ApiKey = apiKey,
            BaseUrl = "https://api-inference.huggingface.co",
            VisionModel = "test/model",
            VisionMaxNewTokens = 100
        });
        var logger = new Mock<ILogger<HuggingFaceClient>>().Object;
        return new HuggingFaceClient(httpClient, options, logger);
    }

    private sealed class MockHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
