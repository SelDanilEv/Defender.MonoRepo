using Defender.PersonalFoodAdvisor.Application.Kafka;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;

public interface IMenuParsingKafkaPublisher
{
    Task PublishAsync(MenuParsingRequestedEvent evt, CancellationToken cancellationToken = default);
}
