using System.Text.Json.Serialization;
using Defender.BudgetTracker.Domain.Enums;

namespace Defender.BudgetTracker.Application.Models.RegularExpenseReviews;

public class RegularExpenseReviewItemRequest
{
    public Guid RegularExpenseId { get; set; }

    public long Amount { get; set; }

    // Optional client metadata is accepted for contract symmetry, but publish logic always snapshots current definition metadata.
    public string? Name { get; set; }

    public RegularExpenseType? Type { get; set; }

    public Currency? Currency { get; set; }

    public int? OrderPriority { get; set; }

    [JsonIgnore]
    public long DefaultAmount
    {
        get => Amount;
        set => Amount = value;
    }
}
