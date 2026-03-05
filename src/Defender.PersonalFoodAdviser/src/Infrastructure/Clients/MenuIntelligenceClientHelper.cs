using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using Defender.PersonalFoodAdviser.Infrastructure.Configuration.Options;

namespace Defender.PersonalFoodAdviser.Infrastructure.Clients;

internal static class MenuIntelligenceClientHelper
{
    public const string DefaultVisionPrompt = MenuIntelligenceOptions.DefaultVisionPrompt;
    public const string VisionItemsPropertyName = "items";
    public const string RankedItemsPropertyName = "rankedItems";

    public static string ResolveVisionPrompt(string? prompt)
        => string.IsNullOrWhiteSpace(prompt) ? DefaultVisionPrompt : prompt.Trim();

    public static string BuildRecommendationPrompt(
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
            sb.Append("Previous ratings (1 = strong dislike, 5 = strong like): ");
            sb.AppendLine(string.Join(", ", ratingHistory.Select(r => $"{r.DishName}({r.Rating})")));
            sb.Append("Use rating history as the strongest signal. ");
            sb.Append("Do not recommend dishes previously rated 1 or 2 if any unrated or higher-rated alternatives exist. ");
            sb.Append("Strongly deprioritize dishes rated 3. ");
            sb.Append("Prefer dishes previously rated 4 or 5 and unrated dishes that are similar to those highly rated dishes. ");
        }

        sb.Append("Try something new: ").AppendLine(trySomethingNew ? "yes" : "no");
        sb.Append($"Return only valid JSON in the exact shape {{\"{RankedItemsPropertyName}\":[\"Dish 1\",\"Dish 2\"]}}. ");
        sb.Append($"The {RankedItemsPropertyName} array must contain exactly {topN} dish names from the menu, ranked by how much the user will enjoy them. ");
        sb.Append("No markdown, no commentary, no numbers, and no extra text.");
        return sb.ToString();
    }

    public static List<string> ExtractRankedDishesFromTexts(IEnumerable<string> texts)
    {
        var ranked = new List<string>();

        foreach (var text in texts)
        {
            var jsonDishes = ParseStringArrayFromJson(text, RankedItemsPropertyName, VisionItemsPropertyName, "rankedDishes", "dishes");
            if (jsonDishes.Count > 0)
            {
                ranked.AddRange(jsonDishes);
                continue;
            }

            var lines = text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var dish = line.Trim();
                if (string.IsNullOrEmpty(dish) || dish.StartsWith('#'))
                    continue;

                var numPrefix = Regex.Match(dish, @"^\d+[\.\)]\s*").Value;
                if (!string.IsNullOrEmpty(numPrefix))
                    dish = dish[numPrefix.Length..].Trim();

                if (!string.IsNullOrEmpty(dish))
                    ranked.Add(dish);
            }
        }

        return ranked;
    }

    public static IEnumerable<string> ParseDishNamesFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var jsonDishes = ParseStringArrayFromJson(text, VisionItemsPropertyName, "dishes", "dishNames", "menuItems");
        if (jsonDishes.Count > 0)
        {
            foreach (var dish in jsonDishes)
                yield return dish;

            yield break;
        }

        var normalized = text
            .Replace("|", "\n", StringComparison.Ordinal)
            .Replace("Ã¢â‚¬Â¢", "\n", StringComparison.Ordinal)
            .Replace(" - ", "\n", StringComparison.Ordinal)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
        var raw = normalized.Split(['\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in raw)
        {
            var dish = NormalizeDishName(segment);
            if (string.IsNullOrWhiteSpace(dish))
                continue;

            yield return dish;
        }
    }

    public static IReadOnlyList<string> NormalizeRankedDishes(
        IReadOnlyList<string> rankedDishes,
        IReadOnlyList<string> confirmedDishes,
        int topN)
    {
        var confirmedLookup = BuildConfirmedDishLookup(confirmedDishes);
        var normalized = new List<string>(Math.Min(topN, confirmedLookup.Count));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dish in rankedDishes)
        {
            var matchedDish = ResolveConfirmedDish(dish, confirmedLookup);
            if (string.IsNullOrWhiteSpace(matchedDish) || !seen.Add(matchedDish))
                continue;

            normalized.Add(matchedDish);
            if (normalized.Count == topN)
                return normalized;
        }

        foreach (var confirmedDish in confirmedDishes)
        {
            var value = confirmedDish?.Trim();
            if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
                continue;

            normalized.Add(value);
            if (normalized.Count == topN)
                break;
        }

        return normalized;
    }

    private static Dictionary<string, string> BuildConfirmedDishLookup(IReadOnlyList<string> confirmedDishes)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var confirmedDish in confirmedDishes)
        {
            var value = confirmedDish?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var key = NormalizeDishComparisonKey(value);
            if (!string.IsNullOrWhiteSpace(key) && !lookup.ContainsKey(key))
                lookup[key] = value;
        }

        return lookup;
    }

    private static string ResolveConfirmedDish(string? rankedDish, IReadOnlyDictionary<string, string> confirmedLookup)
    {
        var value = rankedDish?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var key = NormalizeDishComparisonKey(value);
        if (!string.IsNullOrWhiteSpace(key) && confirmedLookup.TryGetValue(key, out var confirmedDish))
            return confirmedDish;

        return ResolveClosestConfirmedDish(key, confirmedLookup);
    }

    private static string NormalizeDishName(string? value)
    {
        var dish = value?.Trim();
        if (string.IsNullOrWhiteSpace(dish))
            return string.Empty;

        dish = Regex.Replace(dish, @"^\d+[\.\)]\s*", "");
        dish = Regex.Replace(dish, @"\s*\d+[\.\)]\s*$", "");
        dish = Regex.Replace(dish, @"\s*\d+[\.,]\d{2}\s*$", "");
        dish = Regex.Replace(dish, @"\s*\d+\s*$", "");
        dish = Regex.Replace(dish, @"^\s*[-Ã¢â‚¬â€œÃ¢â‚¬â€]\s*", "");
        dish = Regex.Replace(dish, @"\s+", " ").Trim();

        return dish.Length < 2 || Regex.IsMatch(dish, @"^\d+$")
            ? string.Empty
            : dish;
    }

    private static string NormalizeDishComparisonKey(string? value)
    {
        var dish = NormalizeDishName(value);
        if (string.IsNullOrWhiteSpace(dish))
            return string.Empty;

        var decomposed = dish.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        var comparisonKey = builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();

        comparisonKey = comparisonKey.Replace("&", "+", StringComparison.Ordinal);
        comparisonKey = Regex.Replace(comparisonKey, @"\s*([/+])\s*", "$1");
        comparisonKey = Regex.Replace(comparisonKey, @"[^\p{L}\p{Nd}/+]+", " ");
        comparisonKey = Regex.Replace(comparisonKey, @"\s+", " ").Trim();

        return comparisonKey;
    }

    private static string ResolveClosestConfirmedDish(string rankedKey, IReadOnlyDictionary<string, string> confirmedLookup)
    {
        if (string.IsNullOrWhiteSpace(rankedKey) || confirmedLookup.Count == 0)
            return string.Empty;

        var bestScore = 0d;
        var secondBestScore = 0d;
        var bestMatch = string.Empty;

        foreach (var confirmedDish in confirmedLookup)
        {
            var score = CalculateSimilarityScore(rankedKey, confirmedDish.Key);
            if (score > bestScore)
            {
                secondBestScore = bestScore;
                bestScore = score;
                bestMatch = confirmedDish.Value;
                continue;
            }

            if (score > secondBestScore)
                secondBestScore = score;
        }

        return bestScore >= 0.82d && bestScore - secondBestScore >= 0.08d
            ? bestMatch
            : string.Empty;
    }

    private static double CalculateSimilarityScore(string left, string right)
    {
        if (string.Equals(left, right, StringComparison.Ordinal))
            return 1d;

        var maxLength = Math.Max(left.Length, right.Length);
        if (maxLength == 0)
            return 1d;

        var distance = ComputeLevenshteinDistance(left, right);
        return 1d - (double)distance / maxLength;
    }

    private static int ComputeLevenshteinDistance(string left, string right)
    {
        var distances = new int[left.Length + 1, right.Length + 1];

        for (var i = 0; i <= left.Length; i++)
            distances[i, 0] = i;

        for (var j = 0; j <= right.Length; j++)
            distances[0, j] = j;

        for (var i = 1; i <= left.Length; i++)
        {
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(
                        distances[i - 1, j] + 1,
                        distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[left.Length, right.Length];
    }

    private static List<string> ParseStringArrayFromJson(string text, params string[] propertyNames)
    {
        var payload = ExtractJsonPayload(text);
        if (string.IsNullOrWhiteSpace(payload))
            return [];

        try
        {
            using var document = JsonDocument.Parse(payload);
            return ExtractStringValues(document.RootElement, propertyNames)
                .Select(NormalizeDishName)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IEnumerable<string> ExtractStringValues(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var value in ExtractStringValue(item))
                    yield return value;
            }

            yield break;
        }

        if (element.ValueKind != JsonValueKind.Object)
            yield break;

        var matchedProperty = false;
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetProperty(element, propertyName, out var propertyValue))
                continue;

            matchedProperty = true;
            foreach (var value in ExtractStringValues(propertyValue, propertyNames))
                yield return value;
        }

        if (!matchedProperty)
            yield break;
    }

    private static IEnumerable<string> ExtractStringValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                yield return element.GetString() ?? string.Empty;
                break;
            case JsonValueKind.Object:
                foreach (var propertyName in new[] { "name", "dishName", "title", "value" })
                {
                    if (!TryGetProperty(element, propertyName, out var propertyValue) || propertyValue.ValueKind != JsonValueKind.String)
                        continue;

                    yield return propertyValue.GetString() ?? string.Empty;
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var value in ExtractStringValue(item))
                        yield return value;
                }

                break;
        }
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement propertyValue)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                propertyValue = property.Value;
                return true;
            }
        }

        propertyValue = default;
        return false;
    }

    private static string ExtractJsonPayload(string text)
    {
        var payload = text.Trim();
        if (string.IsNullOrWhiteSpace(payload))
            return string.Empty;

        if (payload.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineBreak = payload.IndexOf('\n');
            var lastFence = payload.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineBreak >= 0 && lastFence > firstLineBreak)
                payload = payload[(firstLineBreak + 1)..lastFence].Trim();
        }

        if (payload.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            payload = payload[4..].TrimStart();

        if (payload.StartsWith("{", StringComparison.Ordinal) || payload.StartsWith("[", StringComparison.Ordinal))
            return payload;

        var objectStart = payload.IndexOf('{');
        var objectEnd = payload.LastIndexOf('}');
        if (objectStart >= 0 && objectEnd > objectStart)
            return payload[objectStart..(objectEnd + 1)];

        var arrayStart = payload.IndexOf('[');
        var arrayEnd = payload.LastIndexOf(']');
        if (arrayStart >= 0 && arrayEnd > arrayStart)
            return payload[arrayStart..(arrayEnd + 1)];

        return string.Empty;
    }
}
