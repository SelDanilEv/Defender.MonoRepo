using Defender.BudgetTracker.Domain.Enums;

namespace Defender.BudgetTracker.Application.Models.RegularExpenses;

public record UpdateRegularExpenseRequest
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public RegularExpenseType? Type { get; set; }

    public Currency? Currency { get; set; }

    public long? DefaultAmount { get; set; }

    public int? OrderPriority { get; set; }
}
