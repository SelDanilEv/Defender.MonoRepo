using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Helpers.LocalSecretHelper;
using Defender.PersonalFoodAdviser.Domain.Enums;
using Defender.PersonalFoodAdviser.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdviser.Infrastructure.Clients.Gemini;

public class GeminiClient(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    IOptions<MenuIntelligenceOptions> menuIntelligenceOptions,
    IGeminiModelFallbackService modelFallbackService,
    ILogger<GeminiClient> logger) : IMenuIntelligenceClient
{
    public async Task<IReadOnlyList<string>> ExtractDishNamesFromImagesAsync(
        IReadOnlyList<byte[]> imageBytes,
        CancellationToken cancellationToken = default)
    {
        var allDishes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var opts = options.Value;
        var apiKey = await ResolveApiKey();
        var prompt = MenuIntelligenceClientHelper.ResolveVisionPrompt(menuIntelligenceOptions.Value.VisionPrompt);

        logger.LogInformation(
            "Gemini dish extraction started: image count={Count}, ApiKey configured={HasKey}, PreferredModel={Model}",
            imageBytes.Count,
            !string.IsNullOrWhiteSpace(apiKey),
            opts.VisionModel);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Gemini ApiKey not configured; menu extraction requires a valid API key.");
            return [];
        }

        for (var i = 0; i < imageBytes.Count; i++)
        {
            var bytes = imageBytes[i];
            try
            {
                var texts = await GenerateTextPartsAsync(
                    apiKey,
                    GeminiModelRoute.Vision,
                    CreateVisionPayload(bytes, prompt, opts),
                    cancellationToken);

                foreach (var text in texts)
                {
                    foreach (var dish in MenuIntelligenceClientHelper.ParseDishNamesFromText(text))
                    {
                        allDishes.Add(dish.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Gemini dish extraction failed for image index={Index}; continuing", i);
            }
        }

        var result = allDishes.ToList();
        logger.LogInformation("Gemini dish extraction finished: total unique dishes={Count}", result.Count);
        return result;
    }

    public async Task<IReadOnlyList<string>> GetRankedRecommendationsAsync(
        IReadOnlyList<string> confirmedDishes,
        IReadOnlyList<string> likes,
        IReadOnlyList<string> dislikes,
        IReadOnlyList<(string DishName, int Rating)> ratingHistory,
        bool trySomethingNew,
        int topN,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var apiKey = await ResolveApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Gemini ApiKey not configured; returning placeholder recommendations");
            return MenuIntelligenceClientHelper.NormalizeRankedDishes(confirmedDishes, confirmedDishes, topN);
        }

        var prompt = MenuIntelligenceClientHelper.BuildRecommendationPrompt(
            confirmedDishes,
            likes,
            dislikes,
            ratingHistory,
            trySomethingNew,
            topN);

        var texts = await GenerateTextPartsAsync(
            apiKey,
            GeminiModelRoute.Recommendation,
            CreateRecommendationPayload(prompt, opts),
            cancellationToken);

        var ranked = MenuIntelligenceClientHelper.ExtractRankedDishesFromTexts(texts);
        return MenuIntelligenceClientHelper.NormalizeRankedDishes(ranked, confirmedDishes, topN);
    }

    private async Task<string> ResolveApiKey()
    {
        if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
            return options.Value.ApiKey;

        try
        {
            return await LocalSecretsHelper.GetSecretAsync(LocalSecret.GeminiApiKey);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Gemini API key secret lookup failed");
            return string.Empty;
        }
    }

    private async Task<IReadOnlyList<string>> GenerateTextPartsAsync<TPayload>(
        string apiKey,
        GeminiModelRoute route,
        TPayload payload,
        CancellationToken cancellationToken)
        where TPayload : notnull
    {
        return await modelFallbackService.ExecuteAsync(
            route,
            async (model, ct) =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"models/{model}:generateContent");
                request.Headers.Add("x-goog-api-key", apiKey);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var response = await httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(ct);
                return ParseTextPartsFromResponse(json);
            },
            cancellationToken);
    }

    private static GeminiGenerateContentRequest CreateVisionPayload(
        byte[] imageBytes,
        string prompt,
        GeminiOptions options)
        => new()
        {
            Contents =
            [
                new GeminiContent
                {
                    Parts =
                    [
                        new GeminiPart
                        {
                            InlineData = new GeminiInlineData
                            {
                                MimeType = "image/jpeg",
                                Data = Convert.ToBase64String(imageBytes)
                            }
                        },
                        new GeminiPart
                        {
                            Text = prompt
                        }
                    ]
                }
            ],
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = options.VisionTemperature
            }
        };

    private static GeminiGenerateContentRequest CreateRecommendationPayload(string prompt, GeminiOptions options)
        => new()
        {
            Contents =
            [
                new GeminiContent
                {
                    Parts =
                    [
                        new GeminiPart
                        {
                            Text = prompt
                        }
                    ]
                }
            ],
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = options.RecommendationTemperature
            }
        };

    private static IReadOnlyList<string> ParseTextPartsFromResponse(string json)
    {
        var result = new List<string>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var content)
                    || !content.TryGetProperty("parts", out var parts)
                    || parts.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text))
                        result.Add(text.GetString() ?? string.Empty);
                }
            }
        }
        catch
        {
            result.Add(json);
        }

        return result;
    }

    private sealed class GeminiGenerateContentRequest
    {
        [JsonPropertyName("contents")]
        public required IReadOnlyList<GeminiContent> Contents { get; init; }

        [JsonPropertyName("generationConfig")]
        public required GeminiGenerationConfig GenerationConfig { get; init; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public required IReadOnlyList<GeminiPart> Parts { get; init; }
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("inlineData")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiInlineData? InlineData { get; init; }

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; init; }
    }

    private sealed class GeminiInlineData
    {
        [JsonPropertyName("mimeType")]
        public required string MimeType { get; init; }

        [JsonPropertyName("data")]
        public required string Data { get; init; }
    }

    private sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("maxOutputTokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxOutputTokens { get; init; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; init; }
    }
}
