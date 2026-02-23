namespace Defender.PersonalFoodAdviser.Application.Configuration.Options;

public class HuggingFaceOptions
{
    public const string SectionName = "HuggingFaceOptions";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api-inference.huggingface.co";
    public string VisionModel { get; set; } = "Salesforce/blip2-opt-2.7b";
    public int VisionMaxNewTokens { get; set; } = 400;
    public string VisionPrompt { get; set; } = "List all dish and menu item names visible in this restaurant menu. Return only the names, one per line, no prices or numbers.";
    public string TextModel { get; set; } = "google/flan-t5-base";
}
