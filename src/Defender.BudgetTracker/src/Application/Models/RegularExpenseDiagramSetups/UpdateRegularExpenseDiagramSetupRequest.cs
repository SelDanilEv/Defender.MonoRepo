using Defender.BudgetTracker.Domain.Entities.DiagramSetup;
using Defender.BudgetTracker.Domain.Enums;

namespace Defender.BudgetTracker.Application.Models.RegularExpenseDiagramSetups;

public record UpdateRegularExpenseDiagramSetupRequest
{
    public Currency MainCurrency { get; set; }

    public int? LastMonths { get; set; }

    public DateOnly? EndMonth { get; set; }

    public RegularExpenseDiagramSetup MapToSetup(Guid userId) => new()
    {
        UserId = userId,
        MainCurrency = MainCurrency,
        LastMonths = LastMonths,
        EndMonth = EndMonth
    };
}
