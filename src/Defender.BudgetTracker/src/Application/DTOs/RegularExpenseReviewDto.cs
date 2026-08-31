using Defender.BudgetTracker.Domain.Entities.Rates;
using Defender.BudgetTracker.Domain.Enums;

namespace Defender.BudgetTracker.Application.DTOs;

public class ReviewedRegularExpenseDto
{
    public Guid RegularExpenseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public RegularExpenseType Type { get; set; }
    public Currency Currency { get; set; }
    public long Amount { get; set; }
    public int OrderPriority { get; set; }
    public decimal MonthlyContribution { get; set; }
}

public class RegularExpenseReviewDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Month { get; set; }
    public List<ReviewedRegularExpenseDto> Expenses { get; set; } = [];
    public RatesModel RatesModel { get; set; } = new();
}
