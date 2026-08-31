using Defender.BudgetTracker.Application.Models.RegularExpenseDiagramSetups;
using Defender.BudgetTracker.Domain.Entities.DiagramSetup;

namespace Defender.BudgetTracker.Application.Common.Interfaces.Services;

public interface IRegularExpenseDiagramSetupService
{
    Task<RegularExpenseDiagramSetup> GetCurrentUserRegularExpenseDiagramSetupAsync();

    Task<RegularExpenseDiagramSetup> UpdateRegularExpenseDiagramSetupAsync(
        UpdateRegularExpenseDiagramSetupRequest request);
}
