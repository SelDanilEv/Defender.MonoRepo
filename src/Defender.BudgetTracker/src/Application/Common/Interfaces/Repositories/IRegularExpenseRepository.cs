using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;

namespace Defender.BudgetTracker.Application.Common.Interfaces.Repositories;

public interface IRegularExpenseRepository
{
    Task<PagedResult<RegularExpense>> GetRegularExpensesAsync(
        PaginationRequest pagination,
        Guid userId);

    Task<RegularExpense?> GetRegularExpenseAsync(Guid userId, Guid expenseId);

    Task<RegularExpense> CreateRegularExpenseAsync(RegularExpense newExpense);

    Task<RegularExpense> UpdateRegularExpenseAsync(
        UpdateModelRequest<RegularExpense> request,
        Guid userId);

    Task DeleteRegularExpenseAsync(Guid expenseId, Guid userId);
}
