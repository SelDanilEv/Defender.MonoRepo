namespace Defender.PersonalFoodAdviser.Application.Kafka;

public static class KafkaTopicNames
{
    public const string MenuParsingRequested = "personal-food-adviser_menu-parsing-requested";
    public const string MenuParsed = "personal-food-adviser_menu-parsed";
    public const string RecommendationsRequested = "personal-food-adviser_recommendations-requested";
    public const string RecommendationsGenerated = "personal-food-adviser_recommendations-generated";
}
