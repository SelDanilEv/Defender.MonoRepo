using Defender.Portal.Application.Enums;

namespace Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;

public record CreateRegularExpenseRequest
{
    public string Name { get; set; } = string.Empty;
    public RegularExpenseType Type { get; set; }
    public Currency Currency { get; set; }
    public long DefaultAmount { get; set; }
    public int OrderPriority { get; set; } = -1;
}
