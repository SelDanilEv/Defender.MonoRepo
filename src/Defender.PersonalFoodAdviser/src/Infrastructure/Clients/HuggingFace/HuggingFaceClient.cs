using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Configuration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdviser.Infrastructure.Clients.HuggingFace;

public class HuggingFaceClient(
    HttpClient httpClient,
    IOptions<HuggingFaceOptions> options,
    ILogger<HuggingFaceClient> logger) : IHuggingFaceClient
{
    public async Task<IReadOnlyList<string>> ExtractDishNamesFromImagesAsync(IReadOnlyList<byte[]> imageBytes, CancellationToken cancellationToken = default)
    {
        var allDishes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var opts = options.Value;

        logger.LogInformation(
            "ExtractDishNamesFromImages: image count={Count}, ApiKey configured={HasKey}, VisionModel={Model}",
            imageBytes.Count, !string.IsNullOrEmpty(opts.ApiKey), opts.VisionModel ?? "(null)");

        if (string.IsNullOrEmpty(opts.ApiKey))
        {
            logger.LogWarning("HuggingFace ApiKey not configured; menu extraction requires a valid API key. Configure HuggingFaceOptions:ApiKey.");
            return [];
        }
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);
        var visionUrl = $"{opts.BaseUrl.TrimEnd('/')}/models/{opts.VisionModel}";
        var prompt = string.IsNullOrWhiteSpace(opts.VisionPrompt)
            ? "List all dish and menu item names visible in this restaurant menu. Return only the names, one per line, no prices or numbers."
            : opts.VisionPrompt.Trim();

        for (var i = 0; i < imageBytes.Count; i++)
        {
            var bytes = imageBytes[i];
            try
            {
                var json = await CallVisionApiAsync(visionUrl, bytes, prompt, opts.VisionMaxNewTokens, cancellationToken);
                var texts = ParseGeneratedTextFromResponse(json);
                if (texts.Count == 0 && !string.IsNullOrWhiteSpace(json))
                    logger.LogWarning(
                        "ExtractDishNamesFromImages: image index={Index}, API returned {JsonLength} chars but no generated_text/summary_text found. Json preview: {Preview}",
                        i, json.Length, json.Length > 300 ? json[..300] + "..." : json);
                logger.LogInformation(
                    "ExtractDishNamesFromImages: image index={Index}, response produced {TextCount} text segment(s), total raw length={RawLength}",
                    i, texts.Count, texts.Sum(t => t?.Length ?? 0));

                var imageDishCount = 0;
                foreach (var text in texts)
                {
                    var dishes = ParseDishNamesFromText(text).ToList();
                    if (!string.IsNullOrWhiteSpace(text) && dishes.Count == 0)
                        logger.LogWarning(
                            "ExtractDishNamesFromImages: image index={Index} had non-empty model output but ParseDishNamesFromText returned 0 dishes. Raw preview (first 500 chars): {Preview}",
                            i, text.Length > 500 ? text[..500] + "..." : text);
                    foreach (var d in dishes)
                    {
                        if (!string.IsNullOrWhiteSpace(d))
                        {
                            allDishes.Add(d.Trim());
                            imageDishCount++;
                        }
                    }
                }
                logger.LogInformation(
                    "ExtractDishNamesFromImages: image index={Index} yielded {DishCount} dish names from this image (cumulative: {Total})",
                    i, imageDishCount, allDishes.Count);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "ExtractDishNamesFromImages: failed to process image index={Index} (size={Size} bytes); continuing with remaining images", i, bytes.Length);
            }
        }

        var result = allDishes.ToList();
        logger.LogInformation(
            "ExtractDishNamesFromImages: finished. Total unique dishes={Count}. Dishes: [{Dishes}]",
            result.Count, string.Join(", ", result));
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
        if (string.IsNullOrEmpty(opts.ApiKey))
        {
            logger.LogWarning("HuggingFace ApiKey not configured; returning placeholder recommendations");
            return confirmedDishes.Take(topN).ToList();
        }
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);
        var prompt = BuildRecommendationPrompt(confirmedDishes, likes, dislikes, ratingHistory, trySomethingNew, topN);
        var textUrl = $"{opts.BaseUrl.TrimEnd('/')}/models/{opts.TextModel}";
        var body = JsonSerializer.Serialize(new { inputs = prompt });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(textUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var texts = ParseGeneratedTextFromResponse(json);
        var ranked = new List<string>();
        foreach (var text in texts)
        {
            var lines = text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var dish = line.Trim();
                if (string.IsNullOrEmpty(dish) || dish.StartsWith('#')) continue;
                var numPrefix = Regex.Match(dish, @"^\d+[\.\)]\s*").Value;
                if (!string.IsNullOrEmpty(numPrefix))
                    dish = dish.Substring(numPrefix.Length).Trim();
                if (!string.IsNullOrEmpty(dish))
                    ranked.Add(dish);
            }
        }
        return ranked.Take(topN).ToList();
    }

    private static string BuildRecommendationPrompt(
        IReadOnlyList<string> confirmedDishes,
        IReadOnlyList<string> likes,
        IReadOnlyList<string> dislikes,
        IReadOnlyList<(string DishName, int Rating)> ratingHistory,
        bool trySomethingNew,
        int topN)
    {
        var sb = new StringBuilder();
        sb.Append("Menu dishes: ").AppendLine(string.Join(", ", confirmedDishes));
        if (likes.Count > 0) sb.Append("Likes: ").AppendLine(string.Join(", ", likes));
        if (dislikes.Count > 0) sb.Append("Dislikes: ").AppendLine(string.Join(", ", dislikes));
        if (ratingHistory.Count > 0)
        {
            sb.Append("Previous ratings: ");
            sb.AppendLine(string.Join(", ", ratingHistory.Select(r => $"{r.DishName}({r.Rating})")));
        }
        sb.Append("Try something new: ").AppendLine(trySomethingNew ? "yes" : "no");
        sb.Append($"Return exactly {topN} dish names from the menu, one per line, ranked by how much the user will enjoy them. Only dish names, no numbers or extra text.");
        return sb.ToString();
    }

    private async Task<string> CallVisionApiAsync(string visionUrl, byte[] imageBytes, string prompt, int maxNewTokens, CancellationToken cancellationToken)
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
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.UnsupportedMediaType)
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
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.TryGetProperty("generated_text", out var gt))
                        result.Add(gt.GetString() ?? "");
                    else if (el.TryGetProperty("summary_text", out var st))
                        result.Add(st.GetString() ?? "");
                }
            }
            else if (doc.RootElement.TryGetProperty("generated_text", out var gt))
                result.Add(gt.GetString() ?? "");
        }
        catch
        {
            result.Add(json);
        }
        return result;
    }

    private static IEnumerable<string> ParseDishNamesFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        var normalized = text
            .Replace("|", "\n", StringComparison.Ordinal)
            .Replace("•", "\n", StringComparison.Ordinal)
            .Replace(" - ", "\n", StringComparison.Ordinal)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
        var raw = normalized.Split(['\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in raw)
        {
            var dish = segment.Trim();
            if (string.IsNullOrWhiteSpace(dish)) continue;
            dish = Regex.Replace(dish, @"^\d+[\.\)]\s*", "");
            dish = Regex.Replace(dish, @"\s*\d+[\.\)]\s*$", "");
            dish = Regex.Replace(dish, @"\s*\d+[\.,]\d{2}\s*$", "");
            dish = Regex.Replace(dish, @"\s*\d+\s*$", "");
            dish = Regex.Replace(dish, @"^\s*[-–—]\s*", "");
            dish = Regex.Replace(dish, @"\s+", " ").Trim();
            if (dish.Length < 2) continue;
            if (Regex.IsMatch(dish, @"^\d+$")) continue;
            yield return dish;
        }
    }
}
