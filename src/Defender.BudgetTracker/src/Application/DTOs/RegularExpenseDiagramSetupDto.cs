using Defender.BudgetTracker.Domain.Enums;

namespace Defender.BudgetTracker.Application.DTOs;

public class RegularExpenseDiagramSetupDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Currency MainCurrency { get; set; }
    public int? LastMonths { get; set; }
    public DateOnly? EndMonth { get; set; }
}
