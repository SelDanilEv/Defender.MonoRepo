using Defender.PersonalFoodAdviser.Application.Kafka;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;

public interface IMenuParsingOutboxService
{
    Task EnqueueAsync(MenuParsingRequestedEvent evt, CancellationToken cancellationToken = default);
}
