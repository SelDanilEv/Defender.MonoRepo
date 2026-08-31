using Defender.BudgetTracker.Application.Models.RegularExpenses;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.Common.DB.Pagination;

namespace Defender.BudgetTracker.Application.Common.Interfaces.Services;

public interface IRegularExpenseService
{
    Task<PagedResult<RegularExpense>> GetCurrentUserRegularExpensesAsync(
        PaginationRequest paginationRequest);

    Task<RegularExpense> CreateRegularExpenseAsync(CreateRegularExpenseRequest request);

    Task<RegularExpense> UpdateRegularExpenseAsync(UpdateRegularExpenseRequest request);

    Task<Guid> DeleteRegularExpenseAsync(Guid expenseId);
}
