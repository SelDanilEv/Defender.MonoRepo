using Defender.Portal.Application.Enums;

namespace Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;

public class PortalRegularExpenseDiagramSetup
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Currency MainCurrency { get; set; }
    public int? LastMonths { get; set; }
    public DateOnly? EndMonth { get; set; }
}
