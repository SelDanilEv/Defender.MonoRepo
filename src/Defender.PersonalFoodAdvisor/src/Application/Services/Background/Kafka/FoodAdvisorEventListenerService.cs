using Defender.Kafka.Default;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdvisor.Application.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdvisor.Application.Services.Background.Kafka;

public class FoodAdvisorEventListenerService(
    IDefaultKafkaConsumer<MenuParsingRequestedEvent> menuParsingConsumer,
    IDefaultKafkaConsumer<RecommendationsRequestedEvent> recommendationsConsumer,
    IMenuParsingProcessor menuParsingProcessor,
    IRecommendationProcessor recommendationProcessor,
    ILogger<FoodAdvisorEventListenerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Starting FoodAdvisorEventListenerService. Topics: {MenuParsingTopic}, {RecommendationsTopic}; Groups: {ParsingGroup}, {RecommendationsGroup}",
            KafkaTopicNames.MenuParsingRequested,
            KafkaTopicNames.RecommendationsRequested,
            "personal-food-advisor-parsing",
            "personal-food-advisor-recommendations");

        await Task.Delay(5_000, stoppingToken);

        logger.LogInformation("FoodAdvisorEventListenerService delay completed; starting Kafka consumers");

        await Task.WhenAll(
            menuParsingConsumer.StartConsuming(
                KafkaTopicNames.MenuParsingRequested,
                "personal-food-advisor-parsing",
                HandleMenuParsingRequested,
                stoppingToken),
            recommendationsConsumer.StartConsuming(
                KafkaTopicNames.RecommendationsRequested,
                "personal-food-advisor-recommendations",
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
                "Processing RecommendationsRequested for session {SessionId}, user {UserId}, confirmedItemsCount {ConfirmedItemsCount}, trySomethingNew {TrySomethingNew}, attempt {Attempt}",
                evt.SessionId,
                evt.UserId,
                evt.ConfirmedItems.Count,
                evt.TrySomethingNew,
                evt.Attempt);
            await recommendationProcessor.ProcessAsync(evt);
            logger.LogInformation("Processed RecommendationsRequested for session {SessionId}, attempt {Attempt}", evt.SessionId, evt.Attempt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling RecommendationsRequested for session {SessionId}, attempt {Attempt}", evt.SessionId, evt.Attempt);
        }
    }
}
