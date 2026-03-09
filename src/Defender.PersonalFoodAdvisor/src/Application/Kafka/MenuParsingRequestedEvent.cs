namespace Defender.PersonalFoodAdvisor.Application.Kafka;

public record MenuParsingRequestedEvent(Guid SessionId, Guid UserId, IReadOnlyList<string> ImageRefs);
