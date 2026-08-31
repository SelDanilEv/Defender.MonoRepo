using Defender.Portal.Application.Enums;

namespace Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;

public record UpdateRegularExpenseRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public RegularExpenseType? Type { get; set; }
    public Currency? Currency { get; set; }
    public long? DefaultAmount { get; set; }
    public int? OrderPriority { get; set; }
}
