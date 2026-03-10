namespace Defender.PersonalFoodAdvisor.Infrastructure.Configuration.Options;

public class MenuIntelligenceOptions
{
    public const string SectionName = "MenuIntelligenceOptions";
    public const string DefaultVisionPrompt =
        "Extract only standalone menu products that can be ordered directly from this menu photo. The image can be low quality (blur, glare/reflections, compression artifacts, motion blur, noise, perspective tilt, partial occlusion, or decorative fonts), so read carefully and recover text from context when possible. If one product has multiple sizes or variants, return the base product name only once. Ignore size labels and portion variants (for example small/medium/large or maly/sredni/duzy), ingredient lists in parentheses, category headers, extras/add-ons, sauces, toppings, standalone ingredients, prices, and currency symbols. Exclude text that is unreadable or uncertain; include only dish names you can read with high confidence. Return only valid JSON in the exact shape {\"items\":[\"Dish 1\",\"Dish 2\"]}. No markdown, no commentary, no numbers, and no extra text.";

    public MenuIntelligenceProvider Provider { get; set; } = MenuIntelligenceProvider.Gemini;

    public string VisionPrompt { get; set; } = DefaultVisionPrompt;
}
