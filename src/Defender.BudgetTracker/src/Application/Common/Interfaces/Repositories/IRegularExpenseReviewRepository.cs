using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.Common.DB.Pagination;

namespace Defender.BudgetTracker.Application.Common.Interfaces.Repositories;

public interface IRegularExpenseReviewRepository
{
    Task<PagedResult<RegularExpenseReview>> GetRegularExpenseReviewsAsync(
        PaginationRequest pagination,
        Guid userId);

    Task<List<RegularExpenseReview>> GetRegularExpenseReviewsAsync(
        Guid userId,
        DateOnly startMonth,
        DateOnly endMonth);

    Task<RegularExpenseReview?> GetRegularExpenseReviewAsync(Guid userId, DateOnly month);

    Task<RegularExpenseReview?> GetRegularExpenseReviewAsync(Guid userId, Guid reviewId);

    Task<RegularExpenseReview?> GetLatestRegularExpenseReviewAsync(Guid userId);

    Task<RegularExpenseReview> UpsertRegularExpenseReviewAsync(RegularExpenseReview review);

    Task DeleteRegularExpenseReviewAsync(Guid reviewId, Guid userId);
}
