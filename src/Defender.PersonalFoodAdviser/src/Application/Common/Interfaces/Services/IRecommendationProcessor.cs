using Defender.PersonalFoodAdviser.Application.Kafka;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;

public interface IRecommendationProcessor
{
    Task ProcessAsync(RecommendationsRequestedEvent evt, CancellationToken cancellationToken = default);
}
