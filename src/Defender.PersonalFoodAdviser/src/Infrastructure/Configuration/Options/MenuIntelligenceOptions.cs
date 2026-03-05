namespace Defender.PersonalFoodAdviser.Infrastructure.Configuration.Options;

public class MenuIntelligenceOptions
{
    public const string SectionName = "MenuIntelligenceOptions";
    public const string DefaultVisionPrompt =
        "Extract only standalone menu products that can be ordered directly. If one product has multiple sizes or variants, return the base product name only once. Ignore size labels and portion variants (for example small/medium/large or maly/sredni/duzy), ingredient lists in parentheses, category headers, extras/add-ons, sauces, toppings, and standalone ingredients. Return only valid JSON in the exact shape {\"items\":[\"Dish 1\",\"Dish 2\"]}. No markdown, no commentary, no prices, no numbers, and no extra text.";

    public MenuIntelligenceProvider Provider { get; set; } = MenuIntelligenceProvider.Gemini;

    public string VisionPrompt { get; set; } = DefaultVisionPrompt;
}
