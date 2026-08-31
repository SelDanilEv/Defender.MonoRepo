using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.BudgetTracker.Domain.Enums;

namespace Defender.BudgetTracker.Application.Models.RegularExpenses;

public record CreateRegularExpenseRequest
{
    public string Name { get; set; } = string.Empty;

    public RegularExpenseType Type { get; set; }

    public Currency Currency { get; set; }

    public long DefaultAmount { get; set; }

    public int OrderPriority { get; set; } = -1;

    public RegularExpense CreateRegularExpense(Guid userId) => new()
    {
        UserId = userId,
        Name = Name.Trim(),
        Type = Type,
        Currency = Currency,
        DefaultAmount = DefaultAmount,
        OrderPriority = OrderPriority
    };
}
