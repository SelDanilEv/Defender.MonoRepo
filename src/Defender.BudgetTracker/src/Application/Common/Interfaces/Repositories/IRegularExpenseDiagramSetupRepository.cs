using Defender.BudgetTracker.Domain.Entities.DiagramSetup;

namespace Defender.BudgetTracker.Application.Common.Interfaces.Repositories;

public interface IRegularExpenseDiagramSetupRepository
{
    Task<RegularExpenseDiagramSetup?> GetRegularExpenseDiagramSetupByUserIdAsync(Guid userId);

    Task<RegularExpenseDiagramSetup> SetRegularExpenseDiagramSetupAsync(
        RegularExpenseDiagramSetup setup);
}
