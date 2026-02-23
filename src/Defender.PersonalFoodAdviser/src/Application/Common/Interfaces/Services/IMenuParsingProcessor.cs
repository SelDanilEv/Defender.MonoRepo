using Defender.PersonalFoodAdviser.Application.Kafka;

namespace Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;

public interface IMenuParsingProcessor
{
    Task ProcessAsync(MenuParsingRequestedEvent evt, CancellationToken cancellationToken = default);
}
