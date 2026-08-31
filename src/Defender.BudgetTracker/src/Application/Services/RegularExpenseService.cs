using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Models.RegularExpenses;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;
using Defender.Common.Interfaces;

namespace Defender.BudgetTracker.Application.Services;

public class RegularExpenseService(
    IRegularExpenseRepository regularExpenseRepository,
    ICurrentAccountAccessor currentAccountAccessor) : IRegularExpenseService
{
    public Task<PagedResult<RegularExpense>> GetCurrentUserRegularExpensesAsync(
        PaginationRequest paginationRequest)
    {
        return regularExpenseRepository.GetRegularExpensesAsync(
            paginationRequest,
            currentAccountAccessor.GetAccountId());
    }

    public Task<RegularExpense> CreateRegularExpenseAsync(CreateRegularExpenseRequest request)
    {
        var expense = request.CreateRegularExpense(currentAccountAccessor.GetAccountId());

        return regularExpenseRepository.CreateRegularExpenseAsync(expense);
    }

    public Task<RegularExpense> UpdateRegularExpenseAsync(UpdateRegularExpenseRequest request)
    {
        var updateRequest = UpdateModelRequest<RegularExpense>.Init(request.Id)
            .SetIfNotNull(x => x.Name, request.Name?.Trim());

        if (request.Type.HasValue)
        {
            updateRequest.Set(x => x.Type, request.Type.Value);
        }

        if (request.Currency.HasValue)
        {
            updateRequest.Set(x => x.Currency, request.Currency.Value);
        }

        if (request.DefaultAmount.HasValue)
        {
            updateRequest.Set(x => x.DefaultAmount, request.DefaultAmount.Value);
        }

        if (request.OrderPriority.HasValue)
        {
            updateRequest.Set(x => x.OrderPriority, request.OrderPriority.Value);
        }

        return regularExpenseRepository.UpdateRegularExpenseAsync(
            updateRequest,
            currentAccountAccessor.GetAccountId());
    }

    public async Task<Guid> DeleteRegularExpenseAsync(Guid expenseId)
    {
        await regularExpenseRepository.DeleteRegularExpenseAsync(
            expenseId,
            currentAccountAccessor.GetAccountId());

        return expenseId;
    }
}
