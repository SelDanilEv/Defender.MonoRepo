namespace Defender.Portal.Application.Models.ApiRequests.FoodAdviser;

public record ConfirmMenuRequest(List<string> ConfirmedItems, bool TrySomethingNew);
