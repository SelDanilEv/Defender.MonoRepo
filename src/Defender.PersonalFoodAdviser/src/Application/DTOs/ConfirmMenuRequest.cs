namespace Defender.PersonalFoodAdviser.Application.DTOs;

public record ConfirmMenuRequest(IReadOnlyList<string> ConfirmedItems, bool TrySomethingNew);
