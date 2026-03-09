using Defender.PersonalFoodAdvisor.Infrastructure.Clients.HuggingFace;
using Defender.PersonalFoodAdvisor.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Defender.PersonalFoodAdvisor.Tests.Infrastructure.Clients;

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
    public async Task ExtractDishNamesFromImages_WhenApiReturnsJsonItems_ParsesDishNames()
    {
        var json = """[{"generated_text": "{\"items\":[\"Kebab z kurczakiem / wolowina\",\"Kebab z falafelem\",\"Kebab Box\"]}"}]""";
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        Assert.Equal(3, result.Count);
        Assert.Contains("Kebab z kurczakiem / wolowina", result);
        Assert.Contains("Kebab z falafelem", result);
        Assert.Contains("Kebab Box", result);
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

    [Fact]
    public async Task GetRankedRecommendationsAsync_WhenModelReturnsUnknownItems_FiltersToConfirmedDishesAndBackfills()
    {
        var json = """[{"generated_text": "Hallucinated Dish\nCurry\nRamen"}]""";
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.GetRankedRecommendationsAsync(
            ["Ramen", "Curry", "Salad"],
            [],
            [],
            [],
            false,
            3);

        Assert.Equal(["Curry", "Ramen", "Salad"], result);
    }

    [Fact]
    public async Task GetRankedRecommendationsAsync_WhenModelReturnsJsonRankedItems_FiltersToConfirmedDishesAndBackfills()
    {
        var json = """[{"generated_text": "{\"rankedItems\":[\"Hallucinated Dish\",\"Curry\",\"Ramen\"]}"}]""";
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.GetRankedRecommendationsAsync(
            ["Ramen", "Curry", "Salad"],
            [],
            [],
            [],
            false,
            3);

        Assert.Equal(["Curry", "Ramen", "Salad"], result);
    }

    [Fact]
    public async Task GetRankedRecommendationsAsync_WhenModelReturnsEquivalentDishNames_PreservesModelOrder()
    {
        var json = """[{"generated_text": "{\"rankedItems\":[\"Danie z kurczakiem/wolowina\",\"Kebab z falafelem\",\"Kebab box\"]}"}]""";
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.GetRankedRecommendationsAsync(
            ["Kebab Box", "Danie z kurczakiem / wołowiną", "Kebab z falafelem"],
            [],
            [],
            [],
            false,
            3);

        Assert.Equal(["Danie z kurczakiem / wołowiną", "Kebab z falafelem", "Kebab Box"], result);
    }

    [Fact]
    public async Task GetRankedRecommendationsAsync_UsesJsonOnlyPrompt()
    {
        var handler = new MockHttpMessageHandler("""[{"generated_text": "{\"rankedItems\":[\"Curry\",\"Ramen\",\"Salad\"]}"}]""");
        var client = CreateClient(handler, apiKey: "test-key");

        await client.GetRankedRecommendationsAsync(
            ["Ramen", "Curry", "Salad"],
            [],
            [],
            [],
            false,
            3);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("rankedItems", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("Return only valid JSON", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("exactly 3 dish names", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetRankedRecommendationsAsync_WithLowRatingHistory_UsesPromptThatAvoidsBadlyRatedDishes()
    {
        var handler = new MockHttpMessageHandler("""[{"generated_text": "{\"rankedItems\":[\"Baklava\",\"Falafel box\",\"Ayran\"]}"}]""");
        var client = CreateClient(handler, apiKey: "test-key");

        await client.GetRankedRecommendationsAsync(
            ["Kebab box", "Falafel box", "Baklava", "Ayran"],
            [],
            [],
            [("Kebab box", 1), ("Baklava", 5)],
            false,
            3);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("Previous ratings (1 = strong dislike, 5 = strong like)", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("Kebab box(1)", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("Do not recommend dishes previously rated 1 or 2", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("Prefer dishes previously rated 4 or 5", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetRankedRecommendationsAsync_WhenApiKeyMissing_ReturnsNormalizedConfirmedDishes()
    {
        var client = CreateClientWithResponse("[]", apiKey: "");

        var result = await client.GetRankedRecommendationsAsync(
            [" Ramen ", "Curry", "ramen"],
            [],
            [],
            [],
            false,
            10);

        Assert.Equal(["Ramen", "Curry"], result);
    }

    [Fact]
    public async Task ExtractDishNamesFromImages_UsesDefaultPromptThatExcludesSizesAndExtras()
    {
        var handler = new MockHttpMessageHandler("""[{"generated_text": "Kebab"}]""");
        var client = CreateClient(handler, apiKey: "test-key");

        await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("Extract only standalone menu products that can be ordered directly", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("items", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("Return only valid JSON", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("Ignore size labels and portion variants", handler.LastRequestBody, StringComparison.Ordinal);
    }

    private static HuggingFaceClient CreateClientWithResponse(string responseJson, string apiKey)
    {
        var handler = new MockHttpMessageHandler(responseJson);
        return CreateClient(handler, apiKey);
    }

    private static HuggingFaceClient CreateClient(MockHttpMessageHandler handler, string apiKey)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api-inference.huggingface.co/") };
        var options = Options.Create(new HuggingFaceOptions
        {
            ApiKey = apiKey,
            BaseUrl = "https://api-inference.huggingface.co",
            VisionModel = "test/model",
            TextModel = "test/text-model",
            VisionMaxNewTokens = 100
        });
        var menuOptions = Options.Create(new MenuIntelligenceOptions());
        var logger = new Mock<ILogger<HuggingFaceClient>>().Object;
        return new HuggingFaceClient(httpClient, options, menuOptions, logger);
    }

    private sealed class MockHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null
                ? null
                : request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
