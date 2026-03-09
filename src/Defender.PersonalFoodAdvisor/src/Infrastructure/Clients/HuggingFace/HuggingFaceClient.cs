using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Application.Helpers.LocalSecretHelper;
using Defender.PersonalFoodAdvisor.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdvisor.Infrastructure.Clients.HuggingFace;

public class HuggingFaceClient(
    HttpClient httpClient,
    IOptions<HuggingFaceOptions> options,
    IOptions<MenuIntelligenceOptions> menuIntelligenceOptions,
    ILogger<HuggingFaceClient> logger) : IMenuIntelligenceClient
{
    public async Task<IReadOnlyList<string>> ExtractDishNamesFromImagesAsync(
        IReadOnlyList<byte[]> imageBytes,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var apiKey = await ResolveApiKeyAsync();

        logger.LogInformation(
            "ExtractDishNamesFromImages: image count={Count}, ApiKey configured={HasKey}, VisionModel={Model}",
            imageBytes.Count,
            !string.IsNullOrEmpty(apiKey),
            opts.VisionModel ?? "(null)");

        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("HuggingFace ApiKey not configured; menu extraction requires a valid API key. Configure HuggingFaceOptions:ApiKey.");
            return [];
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var visionUrl = $"{opts.BaseUrl.TrimEnd('/')}/models/{opts.VisionModel}";
        var prompt = MenuIntelligenceClientHelper.ResolveVisionPrompt(menuIntelligenceOptions.Value.VisionPrompt);
        var allDishes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < imageBytes.Count; i++)
            await TryProcessVisionImageAsync(i, imageBytes[i], visionUrl, prompt, opts.VisionMaxNewTokens, allDishes, cancellationToken);

        var result = allDishes.ToList();
        logger.LogInformation(
            "ExtractDishNamesFromImages: finished. Total unique dishes={Count}. Dishes: [{Dishes}]",
            result.Count,
            string.Join(", ", result));
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
        var apiKey = await ResolveApiKeyAsync();
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("HuggingFace ApiKey not configured; returning placeholder recommendations");
            return MenuIntelligenceClientHelper.NormalizeRankedDishes(confirmedDishes, confirmedDishes, topN);
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var prompt = MenuIntelligenceClientHelper.BuildRecommendationPrompt(
            confirmedDishes,
            likes,
            dislikes,
            ratingHistory,
            trySomethingNew,
            topN);
        var textUrl = $"{opts.BaseUrl.TrimEnd('/')}/models/{opts.TextModel}";
        var body = JsonSerializer.Serialize(new { inputs = prompt });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(textUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var texts = ParseGeneratedTextFromResponse(json);
        var ranked = MenuIntelligenceClientHelper.ExtractRankedDishesFromTexts(texts);
        return MenuIntelligenceClientHelper.NormalizeRankedDishes(ranked, confirmedDishes, topN);
    }

    private async Task<string> ResolveApiKeyAsync()
    {
        if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
            return options.Value.ApiKey;

        try
        {
            return await LocalSecretsHelper.GetSecretAsync(LocalSecret.HuggingFaceApiKey);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "HuggingFace API key secret lookup failed");
            return string.Empty;
        }
    }

    private async Task TryProcessVisionImageAsync(
        int index,
        byte[] imageBytes,
        string visionUrl,
        string prompt,
        int maxNewTokens,
        ISet<string> allDishes,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = await CallVisionApiAsync(visionUrl, imageBytes, prompt, maxNewTokens, cancellationToken);
            var texts = ParseGeneratedTextFromResponse(json);

            LogEmptyVisionResponse(index, json, texts);
            LogVisionResponseSummary(index, texts);

            var imageDishCount = AddParsedDishes(index, texts, allDishes);
            logger.LogInformation(
                "ExtractDishNamesFromImages: image index={Index} yielded {DishCount} dish names from this image (cumulative: {Total})",
                index,
                imageDishCount,
                allDishes.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ExtractDishNamesFromImages: failed to process image index={Index} (size={Size} bytes); continuing with remaining images", index, imageBytes.Length);
        }
    }

    private void LogEmptyVisionResponse(int index, string json, IReadOnlyCollection<string> texts)
    {
        if (texts.Count > 0 || string.IsNullOrWhiteSpace(json))
            return;

        logger.LogWarning(
            "ExtractDishNamesFromImages: image index={Index}, API returned {JsonLength} chars but no generated_text/summary_text found. Json preview: {Preview}",
            index,
            json.Length,
            json.Length > 300 ? json[..300] + "..." : json);
    }

    private void LogVisionResponseSummary(int index, IReadOnlyCollection<string> texts)
    {
        logger.LogInformation(
            "ExtractDishNamesFromImages: image index={Index}, response produced {TextCount} text segment(s), total raw length={RawLength}",
            index,
            texts.Count,
            texts.Sum(t => t?.Length ?? 0));
    }

    private int AddParsedDishes(int index, IEnumerable<string> texts, ISet<string> allDishes)
    {
        var imageDishCount = 0;

        foreach (var text in texts)
        {
            var dishes = MenuIntelligenceClientHelper.ParseDishNamesFromText(text).ToList();
            LogUnparsedVisionText(index, text, dishes.Count);

            foreach (var dish in dishes.Where(static dish => !string.IsNullOrWhiteSpace(dish)))
            {
                allDishes.Add(dish.Trim());
                imageDishCount++;
            }
        }

        return imageDishCount;
    }

    private void LogUnparsedVisionText(int index, string text, int dishCount)
    {
        if (string.IsNullOrWhiteSpace(text) || dishCount > 0)
            return;

        logger.LogWarning(
            "ExtractDishNamesFromImages: image index={Index} had non-empty model output but ParseDishNamesFromText returned 0 dishes. Raw preview (first 500 chars): {Preview}",
            index,
            text.Length > 500 ? text[..500] + "..." : text);
    }

    private async Task<string> CallVisionApiAsync(
        string visionUrl,
        byte[] imageBytes,
        string prompt,
        int maxNewTokens,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            inputs = prompt,
            image = Convert.ToBase64String(imageBytes),
            parameters = new { max_new_tokens = maxNewTokens },
            options = new { wait_for_model = true }
        };

        using var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(visionUrl, jsonContent, cancellationToken);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest
            || response.StatusCode == System.Net.HttpStatusCode.UnsupportedMediaType)
        {
            var urlWithParams = $"{visionUrl}?max_new_tokens={maxNewTokens}";
            using var binaryContent = new ByteArrayContent(imageBytes);
            binaryContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            response = await httpClient.PostAsync(urlWithParams, binaryContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        response.EnsureSuccessStatusCode();
        return string.Empty;
    }

    private static List<string> ParseGeneratedTextFromResponse(string json)
    {
        var result = new List<string>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("generated_text", out var generatedText))
                        result.Add(generatedText.GetString() ?? string.Empty);
                    else if (element.TryGetProperty("summary_text", out var summaryText))
                        result.Add(summaryText.GetString() ?? string.Empty);
                }
            }
            else if (doc.RootElement.TryGetProperty("generated_text", out var generatedText))
            {
                result.Add(generatedText.GetString() ?? string.Empty);
            }
        }
        catch
        {
            result.Add(json);
        }

        return result;
    }
}
