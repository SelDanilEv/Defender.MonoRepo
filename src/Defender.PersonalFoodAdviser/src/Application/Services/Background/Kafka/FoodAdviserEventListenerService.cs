using Defender.Kafka.Default;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdviser.Application.Services.Background.Kafka;

public class FoodAdviserEventListenerService(
    IDefaultKafkaConsumer<MenuParsingRequestedEvent> menuParsingConsumer,
    IDefaultKafkaConsumer<RecommendationsRequestedEvent> recommendationsConsumer,
    IMenuParsingProcessor menuParsingProcessor,
    IRecommendationProcessor recommendationProcessor,
    ILogger<FoodAdviserEventListenerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Starting FoodAdviserEventListenerService. Topics: {MenuParsingTopic}, {RecommendationsTopic}; Groups: {ParsingGroup}, {RecommendationsGroup}",
            KafkaTopicNames.MenuParsingRequested,
            KafkaTopicNames.RecommendationsRequested,
            "personal-food-adviser-parsing",
            "personal-food-adviser-recommendations");

        await Task.Delay(5_000, stoppingToken);

        logger.LogInformation("FoodAdviserEventListenerService delay completed; starting Kafka consumers");

        await Task.WhenAll(
            menuParsingConsumer.StartConsuming(
                KafkaTopicNames.MenuParsingRequested,
                "personal-food-adviser-parsing",
                HandleMenuParsingRequested,
                stoppingToken),
            recommendationsConsumer.StartConsuming(
                KafkaTopicNames.RecommendationsRequested,
                "personal-food-adviser-recommendations",
                HandleRecommendationsRequested,
                stoppingToken)
        );
    }

    private async Task HandleMenuParsingRequested(MenuParsingRequestedEvent evt)
    {
        try
        {
            logger.LogInformation("Processing MenuParsingRequested for session {SessionId}, user {UserId}, imageRefsCount {ImageRefsCount}", evt.SessionId, evt.UserId, evt.ImageRefs.Count);
            await menuParsingProcessor.ProcessAsync(evt);
            logger.LogInformation("Processed MenuParsingRequested for session {SessionId}", evt.SessionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling MenuParsingRequested for session {SessionId}", evt.SessionId);
        }
    }

    private async Task HandleRecommendationsRequested(RecommendationsRequestedEvent evt)
    {
        try
        {
            logger.LogInformation(
                "Processing RecommendationsRequested for session {SessionId}, user {UserId}, confirmedItemsCount {ConfirmedItemsCount}, trySomethingNew {TrySomethingNew}",
                evt.SessionId,
                evt.UserId,
                evt.ConfirmedItems.Count,
                evt.TrySomethingNew);
            await recommendationProcessor.ProcessAsync(evt);
            logger.LogInformation("Processed RecommendationsRequested for session {SessionId}", evt.SessionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling RecommendationsRequested for session {SessionId}", evt.SessionId);
        }
    }
}
