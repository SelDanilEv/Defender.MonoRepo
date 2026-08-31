using Defender.Portal.Application.Enums;

namespace Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;

public class PortalRegularExpenseRatesModel
{
    public DateOnly Date { get; set; }
    public Currency BaseCurrency { get; set; }
    public Dictionary<Currency, decimal> Rates { get; set; } = [];
}

public class PortalRegularExpenseReview
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Month { get; set; }
    public List<PortalReviewedRegularExpense> Expenses { get; set; } = [];
    public PortalRegularExpenseRatesModel RatesModel { get; set; } = new();
}
