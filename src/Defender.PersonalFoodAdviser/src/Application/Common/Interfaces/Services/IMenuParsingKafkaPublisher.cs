using Defender.PersonalFoodAdviser.Application.Kafka;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;

public interface IMenuParsingKafkaPublisher
{
    Task PublishAsync(MenuParsingRequestedEvent evt, CancellationToken cancellationToken = default);
}
