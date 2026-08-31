using Defender.BudgetTracker.Application.Models.RegularExpenseReviews;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.Common.DB.Pagination;

namespace Defender.BudgetTracker.Application.Common.Interfaces.Services;

public interface IRegularExpenseReviewService
{
    Task<PagedResult<RegularExpenseReview>> GetCurrentUserRegularExpenseReviewsAsync(
        PaginationRequest paginationRequest);

    Task<List<RegularExpenseReview>> GetCurrentUserRegularExpenseReviewsAsync(
        DateOnly startMonth,
        DateOnly endMonth);

    Task<RegularExpenseReview> GetRegularExpenseReviewTemplateAsync(DateOnly? month);

    Task<RegularExpenseReview> PublishRegularExpenseReviewAsync(
        PublishRegularExpenseReviewRequest request);

    Task<Guid> DeleteRegularExpenseReviewAsync(Guid reviewId);
}
