using Defender.PersonalFoodAdvisor.Application.Kafka;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;

public interface IRecommendationProcessor
{
    Task ProcessAsync(RecommendationsRequestedEvent evt, CancellationToken cancellationToken = default);
}
