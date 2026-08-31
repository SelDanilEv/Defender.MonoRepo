using Defender.Portal.Application.Enums;

namespace Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;

public class PortalReviewedRegularExpense
{
    public Guid RegularExpenseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public RegularExpenseType Type { get; set; }
    public Currency Currency { get; set; }
    public long Amount { get; set; }
    public int OrderPriority { get; set; }
    public decimal MonthlyContribution { get; set; }
}
