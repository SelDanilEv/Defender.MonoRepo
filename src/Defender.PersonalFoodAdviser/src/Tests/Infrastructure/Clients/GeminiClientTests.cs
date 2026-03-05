using Defender.PersonalFoodAdviser.Infrastructure.Clients.Gemini;
using Defender.PersonalFoodAdviser.Infrastructure.Configuration.Options;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Defender.PersonalFoodAdviser.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Defender.PersonalFoodAdviser.Tests.Infrastructure.Clients;

public class GeminiClientTests
{
    [Fact]
    public async Task ExtractDishNamesFromImages_WhenApiReturnsCandidateText_ParsesDishNames()
    {
        var json = """
                   {
                     "candidates": [
                       {
                         "content": {
                           "parts": [
                             {
                               "text": "Caesar Salad\nGrilled Salmon\nGarlic Bread"
                             }
                           ]
                         }
                       }
                     ]
                   }
                   """;
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01, 0x02 }]);

        Assert.Equal(3, result.Count);
        Assert.Contains("Caesar Salad", result);
        Assert.Contains("Grilled Salmon", result);
        Assert.Contains("Garlic Bread", result);
    }

    [Fact]
    public async Task ExtractDishNamesFromImages_WhenApiReturnsJsonItems_ParsesDishNames()
    {
        var json = """
                   {
                     "candidates": [
                       {
                         "content": {
                           "parts": [
                             {
                               "text": "{\"items\":[\"Kebab z kurczakiem / wolowina\",\"Kebab z falafelem\",\"Kebab Box\"]}"
                             }
                           ]
                         }
                       }
                     ]
                   }
                   """;
        var client = CreateClientWithResponse(json, apiKey: "test-key");

        var result = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        Assert.Equal(3, result.Count);
        Assert.Contains("Kebab z kurczakiem / wolowina", result);
        Assert.Contains("Kebab z falafelem", result);
        Assert.Contains("Kebab Box", result);
    }

    [Fact]
    public async Task GetRankedRecommendationsAsync_WhenApiReturnsUnknownItems_FiltersToConfirmedDishesAndBackfills()
    {
        var json = """
                   {
                     "candidates": [
                       {
                         "content": {
                           "parts": [
                             {
                               "text": "Hallucinated Dish\nCurry\nRamen"
                             }
                           ]
                         }
                       }
                     ]
                   }
                   """;
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
    public async Task GetRankedRecommendationsAsync_WhenApiReturnsJsonRankedItems_FiltersToConfirmedDishesAndBackfills()
    {
        var json = """
                   {
                     "candidates": [
                       {
                         "content": {
                           "parts": [
                             {
                               "text": "{\"rankedItems\":[\"Hallucinated Dish\",\"Curry\",\"Ramen\"]}"
                             }
                           ]
                         }
                       }
                     ]
                   }
                   """;
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
    public async Task GetRankedRecommendationsAsync_WhenApiReturnsEquivalentDishNames_PreservesModelOrder()
    {
        var json = """
                   {
                     "candidates": [
                       {
                         "content": {
                           "parts": [
                             {
                               "text": "{\"rankedItems\":[\"Danie z kurczakiem/wolowina\",\"Kebab z falafelem\",\"Kebab box\"]}"
                             }
                           ]
                         }
                       }
                     ]
                   }
                   """;
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
        var handler = new MockHttpMessageHandler("""
                                                 {
                                                   "candidates": [
                                                     {
                                                       "content": {
                                                         "parts": [
                                                           {
                                                             "text": "{\"rankedItems\":[\"Curry\",\"Ramen\",\"Salad\"]}"
                                                           }
                                                         ]
                                                       }
                                                     }
                                                   ]
                                                 }
                                                 """);
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
        var handler = new MockHttpMessageHandler("""
                                                 {
                                                   "candidates": [
                                                     {
                                                       "content": {
                                                         "parts": [
                                                           {
                                                             "text": "{\"rankedItems\":[\"Baklava\",\"Falafel box\",\"Ayran\"]}"
                                                           }
                                                         ]
                                                       }
                                                     }
                                                   ]
                                                 }
                                                 """);
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
        var client = CreateClientWithResponse("{}", apiKey: "");

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
        var handler = new MockHttpMessageHandler("""
                                                 {
                                                   "candidates": [
                                                     {
                                                       "content": {
                                                         "parts": [
                                                           {
                                                             "text": "Kebab"
                                                           }
                                                         ]
                                                       }
                                                     }
                                                   ]
                                                 }
                                                 """);
        var client = CreateClient(handler, apiKey: "test-key");

        await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("Extract only standalone menu products that can be ordered directly", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("items", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("Return only valid JSON", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("Ignore size labels and portion variants", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetRankedRecommendationsAsync_WhenPrimaryModelReturnsTooManyRequests_SwitchesModelForNextRequest()
    {
        var handler = new MockHttpMessageHandler(
            CreateResponse(HttpStatusCode.TooManyRequests, """
                                                     {
                                                       "error": {
                                                         "code": 429,
                                                         "message": "rate limited"
                                                       }
                                                     }
                                                     """),
            CreateResponse(HttpStatusCode.OK, """
                                            {
                                              "candidates": [
                                                {
                                                  "content": {
                                                    "parts": [
                                                      {
                                                        "text": "{\"rankedItems\":[\"Curry\",\"Ramen\",\"Salad\"]}"
                                                      }
                                                    ]
                                                  }
                                                }
                                              ]
                                            }
                                            """));
        var client = CreateClient(handler, apiKey: "test-key", configureOptions: opts =>
        {
            opts.RecommendationModel = "gemini-2.5-flash-lite";
            opts.RecommendationFallbackModels = ["gemini-2.5-flash"];
        });

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetRankedRecommendationsAsync(
            ["Ramen", "Curry", "Salad"],
            [],
            [],
            [],
            false,
            3));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);

        var result = await client.GetRankedRecommendationsAsync(
            ["Ramen", "Curry", "Salad"],
            [],
            [],
            [],
            false,
            3);

        Assert.Equal(["Curry", "Ramen", "Salad"], result);
        Assert.Equal(2, handler.RequestUris.Count);
        Assert.Contains(handler.RequestUris, uri => uri.Contains("gemini-2.5-flash-lite:generateContent", StringComparison.Ordinal));
        Assert.Contains(handler.RequestUris, uri => uri.Contains("gemini-2.5-flash:generateContent", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExtractDishNamesFromImages_WhenVisionModelReturnsTooManyRequests_SkipsFailedRequestAndSwitchesOnlyVisionRoute()
    {
        var handler = new MockHttpMessageHandler(
            CreateResponse(HttpStatusCode.TooManyRequests, """
                                                     {
                                                       "error": {
                                                         "code": 429,
                                                         "message": "rate limited"
                                                       }
                                                     }
                                                     """),
            CreateResponse(HttpStatusCode.OK, """
                                            {
                                              "candidates": [
                                                {
                                                  "content": {
                                                    "parts": [
                                                      {
                                                        "text": "Ramen\nCurry"
                                                      }
                                                    ]
                                                  }
                                                }
                                              ]
                                            }
                                            """),
            CreateResponse(HttpStatusCode.OK, """
                                            {
                                              "candidates": [
                                                {
                                                  "content": {
                                                    "parts": [
                                                      {
                                                        "text": "{\"rankedItems\":[\"Curry\",\"Ramen\"]}"
                                                      }
                                                    ]
                                                  }
                                                }
                                              ]
                                            }
                                            """));
        var client = CreateClient(handler, apiKey: "test-key", configureOptions: opts =>
        {
            opts.VisionModel = "gemini-2.5-flash";
            opts.VisionFallbackModels = ["gemini-2.5-flash-lite"];
            opts.RecommendationModel = "gemini-2.5-flash-lite";
            opts.RecommendationFallbackModels = ["gemini-2.5-flash"];
        });

        var firstAttempt = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);
        var dishes = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }]);

        var ranked = await client.GetRankedRecommendationsAsync(
            ["Ramen", "Curry"],
            [],
            [],
            [],
            false,
            2);

        Assert.Empty(firstAttempt);
        Assert.Equal(["Ramen", "Curry"], dishes);
        Assert.Equal(["Curry", "Ramen"], ranked);
        Assert.Equal(3, handler.RequestUris.Count);
        Assert.Contains(handler.RequestUris, uri => uri.Contains("gemini-2.5-flash:generateContent", StringComparison.Ordinal));
        Assert.Equal(2, handler.RequestUris.Count(uri => uri.Contains("gemini-2.5-flash-lite:generateContent", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ExtractDishNamesFromImages_WhenFirstImageIsRateLimited_ContinuesWithRemainingImages()
    {
        var handler = new MockHttpMessageHandler(
            CreateResponse(HttpStatusCode.TooManyRequests, """
                                                     {
                                                       "error": {
                                                         "code": 429,
                                                         "message": "rate limited"
                                                       }
                                                     }
                                                     """),
            CreateResponse(HttpStatusCode.OK, """
                                            {
                                              "candidates": [
                                                {
                                                  "content": {
                                                    "parts": [
                                                      {
                                                        "text": "Tom Yum Soup"
                                                      }
                                                    ]
                                                  }
                                                }
                                              ]
                                            }
                                            """));
        var client = CreateClient(handler, apiKey: "test-key", configureOptions: opts =>
        {
            opts.VisionModel = "gemini-2.5-flash";
            opts.VisionFallbackModels = ["gemini-2.5-flash-lite"];
        });

        var dishes = await client.ExtractDishNamesFromImagesAsync([new byte[] { 0x01 }, new byte[] { 0x02 }]);

        Assert.Equal(["Tom Yum Soup"], dishes);
        Assert.Equal(2, handler.RequestUris.Count);
        Assert.Contains("gemini-2.5-flash:generateContent", handler.RequestUris[0], StringComparison.Ordinal);
        Assert.Contains("gemini-2.5-flash-lite:generateContent", handler.RequestUris[1], StringComparison.Ordinal);
    }

    private static GeminiClient CreateClientWithResponse(string responseJson, string apiKey)
    {
        var handler = new MockHttpMessageHandler(responseJson);
        return CreateClient(handler, apiKey);
    }

    private static GeminiClient CreateClient(MockHttpMessageHandler handler, string apiKey, Action<GeminiOptions>? configureOptions = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/") };
        var geminiOptions = new GeminiOptions
        {
            ApiKey = apiKey,
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta",
            VisionModel = "gemini-2.5-flash",
            VisionFallbackModels = ["gemini-3-flash-preview", "gemini-2.5-flash-lite"],
            RecommendationModel = "gemini-2.5-flash-lite",
            RecommendationFallbackModels = ["gemini-3-flash-preview", "gemini-2.5-flash", "gemma-3-4b-it"],
            ModelSwitchCooldownSeconds = 45
        };
        configureOptions?.Invoke(geminiOptions);
        var options = Options.Create(geminiOptions);
        var menuOptions = Options.Create(new MenuIntelligenceOptions());
        var fallbackLogger = new Mock<ILogger<GeminiModelFallbackService>>().Object;
        var fallbackService = new GeminiModelFallbackService(new FakeGeminiModelLoopStateRepository(), options, TimeProvider.System, fallbackLogger);
        var logger = new Mock<ILogger<GeminiClient>>().Object;
        return new GeminiClient(httpClient, options, menuOptions, fallbackService, logger);
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string responseJson)
        => new(statusCode)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public string? LastRequestBody { get; private set; }
        public List<string> RequestUris { get; } = [];

        public MockHttpMessageHandler(string responseJson)
            : this(CreateResponse(HttpStatusCode.OK, responseJson))
        {
        }

        public MockHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null
                ? null
                : request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            RequestUris.Add(request.RequestUri?.ToString() ?? string.Empty);

            var response = _responses.Count > 1
                ? _responses.Dequeue()
                : _responses.Peek();

            return Task.FromResult(response);
        }
    }

    private sealed class FakeGeminiModelLoopStateRepository : Application.Common.Interfaces.Repositories.IGeminiModelLoopStateRepository
    {
        public Task<IReadOnlyList<GeminiModelLoopState>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<GeminiModelLoopState>>([]);
        }

        public Task UpsertAsync(GeminiModelLoopState state, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
