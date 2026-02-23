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
        await Task.Delay(5_000, stoppingToken);

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
            logger.LogInformation("Processing MenuParsingRequested for session {SessionId}", evt.SessionId);
            await menuParsingProcessor.ProcessAsync(evt);
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
            logger.LogInformation("Processing RecommendationsRequested for session {SessionId}", evt.SessionId);
            await recommendationProcessor.ProcessAsync(evt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling RecommendationsRequested for session {SessionId}", evt.SessionId);
        }
    }
}
