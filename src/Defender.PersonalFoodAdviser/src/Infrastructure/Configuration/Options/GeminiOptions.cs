namespace Defender.PersonalFoodAdviser.Infrastructure.Configuration.Options;

public class GeminiOptions
{
    public const string SectionName = "GeminiOptions";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    public string VisionModel { get; set; } = "gemini-2.5-flash";

    public List<string> VisionFallbackModels { get; set; } =
    [
        "gemini-3-flash-preview",
        "gemini-2.5-flash-lite"
    ];

    public string RecommendationModel { get; set; } = "gemini-2.5-flash-lite";

    public List<string> RecommendationFallbackModels { get; set; } =
    [
        "gemini-3-flash-preview",
        "gemini-2.5-flash",
        "gemma-3-4b-it"
    ];

    public int ModelSwitchCooldownSeconds { get; set; } = 45;

    public double VisionTemperature { get; set; } = 0.2;

    public double RecommendationTemperature { get; set; } = 0.3;
}
