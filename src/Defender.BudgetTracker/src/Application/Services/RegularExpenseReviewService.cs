using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Models.RegularExpenseReviews;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.Common.DB.Pagination;
using Defender.Common.Interfaces;

namespace Defender.BudgetTracker.Application.Services;

public class RegularExpenseReviewService(
    IRegularExpenseReviewRepository regularExpenseReviewRepository,
    IRegularExpenseService regularExpenseService,
    ICurrentAccountAccessor currentAccountAccessor,
    IRatesModelService ratesModelService) : IRegularExpenseReviewService
{
    public Task<PagedResult<RegularExpenseReview>> GetCurrentUserRegularExpenseReviewsAsync(
        PaginationRequest paginationRequest)
    {
        return regularExpenseReviewRepository.GetRegularExpenseReviewsAsync(
            paginationRequest,
            currentAccountAccessor.GetAccountId());
    }

    public Task<List<RegularExpenseReview>> GetCurrentUserRegularExpenseReviewsAsync(
        DateOnly startMonth,
        DateOnly endMonth)
    {
        var normalizedStart = RegularExpenseReview.NormalizeMonth(startMonth);
        var normalizedEnd = RegularExpenseReview.NormalizeMonth(endMonth);

        if (normalizedStart > normalizedEnd)
        {
            throw new ArgumentException("Start month must not be after end month.", nameof(startMonth));
        }

        return regularExpenseReviewRepository.GetRegularExpenseReviewsAsync(
            currentAccountAccessor.GetAccountId(),
            normalizedStart,
            normalizedEnd);
    }

    public async Task<RegularExpenseReview> GetRegularExpenseReviewTemplateAsync(DateOnly? month)
    {
        var normalizedMonth = RegularExpenseReview.NormalizeMonth(
            month ?? DateOnly.FromDateTime(DateTime.UtcNow));
        var userId = currentAccountAccessor.GetAccountId();

        var definitionsTask = regularExpenseService.GetCurrentUserRegularExpensesAsync(
            PaginationRequest.CreateWithoutPagination);
        var latestReviewTask = regularExpenseReviewRepository
            .GetLatestRegularExpenseReviewAsync(userId);
        var ratesTask = ratesModelService.GetRatesModelAsync(normalizedMonth);

        await Task.WhenAll(definitionsTask, latestReviewTask, ratesTask);

        var latestExpenses = latestReviewTask.Result?.Expenses
            .GroupBy(expense => expense.RegularExpenseId)
            .ToDictionary(group => group.Key, group => group.First())
            ?? [];

        return new RegularExpenseReview
        {
            UserId = userId,
            Month = normalizedMonth,
            RatesModel = ratesTask.Result,
            Expenses = definitionsTask.Result.Items
                .Select(definition => CreateSnapshot(
                    definition,
                    latestExpenses.TryGetValue(definition.Id, out var previous)
                        ? previous.Amount
                        : definition.DefaultAmount))
                .ToList()
        };
    }

    public async Task<RegularExpenseReview> PublishRegularExpenseReviewAsync(
        PublishRegularExpenseReviewRequest request)
    {
        var userId = currentAccountAccessor.GetAccountId();
        var normalizedMonth = RegularExpenseReview.NormalizeMonth(request.Month);

        var existingByMonthTask = regularExpenseReviewRepository
            .GetRegularExpenseReviewAsync(userId, normalizedMonth);
        var existingByIdTask = request.Id.HasValue
            ? regularExpenseReviewRepository.GetRegularExpenseReviewAsync(userId, request.Id.Value)
            : Task.FromResult<RegularExpenseReview?>(null);

        await Task.WhenAll(existingByMonthTask, existingByIdTask);

        var existingByMonth = existingByMonthTask.Result;
        var existingById = existingByIdTask.Result;
        var definitions = await regularExpenseService.GetCurrentUserRegularExpensesAsync(
            PaginationRequest.CreateWithoutPagination);

        var requestedAmounts = request.Expenses
            .GroupBy(expense => expense.RegularExpenseId)
            .ToDictionary(group => group.Key, group => group.Last().Amount);
        var currentExpenseIds = definitions.Items
            .Select(definition => definition.Id)
            .ToHashSet();
        var persistedReview = existingByMonth
            ?? (existingById?.Month == normalizedMonth ? existingById : null);
        var persistedReviewExpenses = persistedReview?.Expenses
            .GroupBy(expense => expense.RegularExpenseId)
            .ToDictionary(group => group.Key, group => group.First())
            ?? [];
        var deletedExpenseSnapshots = persistedReviewExpenses.Values
            .Where(expense => !currentExpenseIds.Contains(expense.RegularExpenseId))
            .Select(expense => CreateSnapshot(
                expense,
                requestedAmounts.TryGetValue(expense.RegularExpenseId, out var amount)
                    ? amount
                    : expense.Amount));

        var review = new RegularExpenseReview
        {
            Id = GetReviewId(existingByMonth, existingById, normalizedMonth),
            UserId = userId,
            Month = normalizedMonth,
            RatesModel = persistedReview?.RatesModel
                ?? await ratesModelService.GetRatesModelAsync(normalizedMonth),
            Expenses = definitions.Items
                .Select(definition => CreateSnapshot(
                    definition,
                    requestedAmounts.TryGetValue(definition.Id, out var amount)
                        ? amount
                        : definition.DefaultAmount))
                .Concat(deletedExpenseSnapshots)
                .ToList()
        };

        return await regularExpenseReviewRepository.UpsertRegularExpenseReviewAsync(review);
    }

    public async Task<Guid> DeleteRegularExpenseReviewAsync(Guid reviewId)
    {
        await regularExpenseReviewRepository.DeleteRegularExpenseReviewAsync(
            reviewId,
            currentAccountAccessor.GetAccountId());

        return reviewId;
    }

    private static Guid GetReviewId(
        RegularExpenseReview? existingByMonth,
        RegularExpenseReview? existingById,
        DateOnly month)
    {
        if (existingByMonth is not null)
        {
            return existingByMonth.Id;
        }

        if (existingById is not null && existingById.Month == month)
        {
            return existingById.Id;
        }

        return Guid.NewGuid();
    }

    private static ReviewedRegularExpense CreateSnapshot(
        RegularExpense definition,
        long amount)
    {
        return new ReviewedRegularExpense
        {
            RegularExpenseId = definition.Id,
            Name = definition.Name,
            Type = definition.Type,
            Currency = definition.Currency,
            Amount = amount,
            OrderPriority = definition.OrderPriority
        };
    }

    private static ReviewedRegularExpense CreateSnapshot(
        ReviewedRegularExpense expense,
        long amount)
    {
        return new ReviewedRegularExpense
        {
            RegularExpenseId = expense.RegularExpenseId,
            Name = expense.Name,
            Type = expense.Type,
            Currency = expense.Currency,
            Amount = amount,
            OrderPriority = expense.OrderPriority
        };
    }
}
