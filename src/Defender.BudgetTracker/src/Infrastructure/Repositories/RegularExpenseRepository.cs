using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.Common.Configuration.Options;
using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;
using Defender.Common.DB.Repositories;
using Microsoft.Extensions.Options;

namespace Defender.BudgetTracker.Infrastructure.Repositories;

public class RegularExpenseRepository : BaseMongoRepository<RegularExpense>, IRegularExpenseRepository
{
    public RegularExpenseRepository(IOptions<MongoDbOptions> mongoOption)
        : base(mongoOption.Value, "RegularExpenses")
    {
    }

    public Task<PagedResult<RegularExpense>> GetRegularExpensesAsync(
        PaginationRequest pagination,
        Guid userId)
    {
        var filter = FindModelRequest<RegularExpense>
            .Init(expense => expense.UserId, userId)
            .Sort(expense => expense.OrderPriority, SortType.Desc);
        var settings = PaginationSettings<RegularExpense>
            .FromPaginationRequest(pagination)
            .SetupFindOptions(filter);

        return GetItemsAsync(settings);
    }

    public async Task<RegularExpense?> GetRegularExpenseAsync(Guid userId, Guid expenseId)
    {
        var filter = FindModelRequest<RegularExpense>
            .Init(expense => expense.UserId, userId)
            .And(expense => expense.Id, expenseId);

        return await GetItemAsync(filter);
    }

    public Task<RegularExpense> CreateRegularExpenseAsync(RegularExpense newExpense)
    {
        return AddItemAsync(newExpense);
    }

    public Task<RegularExpense> UpdateRegularExpenseAsync(
        UpdateModelRequest<RegularExpense> request,
        Guid userId)
    {
        var filter = FindModelRequest<RegularExpense>
            .Init(expense => expense.UserId, userId)
            .And(expense => expense.Id, request.ModelId);

        return UpdateItemAsync(request, filter);
    }

    public Task DeleteRegularExpenseAsync(Guid expenseId, Guid userId)
    {
        var filter = FindModelRequest<RegularExpense>
            .Init(expense => expense.UserId, userId)
            .And(expense => expense.Id, expenseId);

        return RemoveItemAsync(expenseId, filter);
    }
}
