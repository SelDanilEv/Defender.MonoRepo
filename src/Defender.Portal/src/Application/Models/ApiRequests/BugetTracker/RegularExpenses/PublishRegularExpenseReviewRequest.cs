using System.Text.Json.Serialization;

namespace Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;

public record PublishRegularExpenseReviewRequest
{
    public Guid? Id { get; set; }
    public DateOnly Month { get; set; }
    public List<RegularExpenseReviewItemRequest> Expenses { get; set; } = [];

    [JsonIgnore]
    public List<RegularExpenseReviewItemRequest> ReviewedExpenses
    {
        get => Expenses;
        set => Expenses = value;
    }
}
