using Defender.PersonalFoodAdvisor.Application.Kafka;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;

public interface IMenuParsingOutboxService
{
    Task EnqueueAsync(MenuParsingRequestedEvent evt, CancellationToken cancellationToken = default);
}
