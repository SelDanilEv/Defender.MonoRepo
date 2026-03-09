namespace Defender.PersonalFoodAdvisor.Application.DTOs;

public record ConfirmMenuRequest(IReadOnlyList<string> ConfirmedItems, bool TrySomethingNew);
