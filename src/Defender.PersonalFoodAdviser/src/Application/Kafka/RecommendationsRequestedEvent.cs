namespace Defender.PersonalFoodAdviser.Application.Kafka;

public record RecommendationsRequestedEvent(Guid SessionId, Guid UserId, IReadOnlyList<string> ConfirmedItems, bool TrySomethingNew);
