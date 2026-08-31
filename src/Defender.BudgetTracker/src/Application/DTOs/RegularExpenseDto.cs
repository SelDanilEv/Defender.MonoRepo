using Defender.BudgetTracker.Domain.Enums;

namespace Defender.BudgetTracker.Application.DTOs;

public class RegularExpenseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public RegularExpenseType Type { get; set; }
    public Currency Currency { get; set; }
    public long DefaultAmount { get; set; }
    public int OrderPriority { get; set; }
}
