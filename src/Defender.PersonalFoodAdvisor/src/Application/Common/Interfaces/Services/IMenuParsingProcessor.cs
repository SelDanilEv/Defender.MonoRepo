using Defender.PersonalFoodAdvisor.Application.Kafka;

namespace Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Services;

public interface IMenuParsingProcessor
{
    Task ProcessAsync(MenuParsingRequestedEvent evt, CancellationToken cancellationToken = default);
}
