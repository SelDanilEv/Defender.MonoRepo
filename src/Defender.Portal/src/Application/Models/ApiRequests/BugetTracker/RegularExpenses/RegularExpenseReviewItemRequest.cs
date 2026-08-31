namespace Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;

public class RegularExpenseReviewItemRequest
{
    public Guid RegularExpenseId { get; set; }
    public long Amount { get; set; }
}
