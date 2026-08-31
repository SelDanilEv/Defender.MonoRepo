using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Models.RegularExpenseDiagramSetups;
using Defender.BudgetTracker.Domain.Entities.DiagramSetup;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.Interfaces;

namespace Defender.BudgetTracker.Application.Services;

public class RegularExpenseDiagramSetupService(
    IRegularExpenseDiagramSetupRepository regularExpenseDiagramSetupRepository,
    ICurrentAccountAccessor currentAccountAccessor) : IRegularExpenseDiagramSetupService
{
    public async Task<RegularExpenseDiagramSetup> GetCurrentUserRegularExpenseDiagramSetupAsync()
    {
        var userId = currentAccountAccessor.GetAccountId();
        var setup = await regularExpenseDiagramSetupRepository
            .GetRegularExpenseDiagramSetupByUserIdAsync(userId);

        return setup ?? new RegularExpenseDiagramSetup
        {
            UserId = userId,
            MainCurrency = Currency.Unknown
        };
    }

    public Task<RegularExpenseDiagramSetup> UpdateRegularExpenseDiagramSetupAsync(
        UpdateRegularExpenseDiagramSetupRequest request)
    {
        return regularExpenseDiagramSetupRepository.SetRegularExpenseDiagramSetupAsync(
            request.MapToSetup(currentAccountAccessor.GetAccountId()));
    }
}
