namespace Defender.PersonalFoodAdviser.Infrastructure.Configuration.Options;

public class HuggingFaceOptions
{
    public const string SectionName = "HuggingFaceOptions";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api-inference.huggingface.co";

    public string VisionModel { get; set; } = "Salesforce/blip2-opt-2.7b";

    public int VisionMaxNewTokens { get; set; } = 400;

    public string TextModel { get; set; } = "google/flan-t5-base";
}
