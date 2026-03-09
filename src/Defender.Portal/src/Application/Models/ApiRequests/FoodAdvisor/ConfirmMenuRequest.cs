namespace Defender.Portal.Application.Models.ApiRequests.FoodAdvisor;

public record ConfirmMenuRequest(List<string> ConfirmedItems, bool TrySomethingNew);
