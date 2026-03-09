using Defender.PersonalFoodAdvisor.Application.Kafka;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;

public interface IRecommendationsKafkaPublisher
{
    Task PublishAsync(RecommendationsRequestedEvent evt, CancellationToken cancellationToken = default);
}
