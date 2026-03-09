namespace Defender.PersonalFoodAdvisor.Application.Kafka;

public static class KafkaTopicNames
{
    public const string MenuParsingRequested = "personal-food-advisor_menu-parsing-requested";
    public const string MenuParsed = "personal-food-advisor_menu-parsed";
    public const string RecommendationsRequested = "personal-food-advisor_recommendations-requested";
    public const string RecommendationsGenerated = "personal-food-advisor_recommendations-generated";
}
