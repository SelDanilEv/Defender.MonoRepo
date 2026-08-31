using Defender.Portal.Application.Enums;

namespace Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;

public record UpdateRegularExpenseDiagramSetupRequest
{
    public Currency MainCurrency { get; set; }
    public int? LastMonths { get; set; }
    public DateOnly? EndMonth { get; set; }
}
