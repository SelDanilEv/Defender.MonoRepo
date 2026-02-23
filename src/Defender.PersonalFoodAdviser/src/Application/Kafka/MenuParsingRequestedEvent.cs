namespace Defender.PersonalFoodAdviser.Application.Kafka;

public record MenuParsingRequestedEvent(Guid SessionId, Guid UserId, IReadOnlyList<string> ImageRefs);
