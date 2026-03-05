using Defender.PersonalFoodAdviser.Application.Kafka;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;

public interface IRecommendationsKafkaPublisher
{
    Task PublishAsync(RecommendationsRequestedEvent evt, CancellationToken cancellationToken = default);
}
