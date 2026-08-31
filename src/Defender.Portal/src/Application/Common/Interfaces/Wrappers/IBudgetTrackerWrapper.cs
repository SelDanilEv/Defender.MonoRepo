using Defender.Common.DB.Pagination;
using Defender.Portal.Application.DTOs.BudgetTracking.DiagramSetup;
using Defender.Portal.Application.DTOs.BudgetTracking.Groups;
using Defender.Portal.Application.DTOs.BudgetTracking.Positions;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using Defender.Portal.Application.DTOs.BudgetTracking.Reviews;
using Defender.Portal.Application.Models.ApiRequests.BugetTracker.BudgetGroups;
using Defender.Portal.Application.Models.ApiRequests.BugetTracker.BudgetReviews;
using Defender.Portal.Application.Models.ApiRequests.BugetTracker.Positions;
using Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;

namespace Defender.Portal.Application.Common.Interfaces.Wrappers;

public interface IBudgetTrackerWrapper
{
    Task<PortalMainDiagramSetup> GetMainDiagramSetupAsync();
    Task<PortalMainDiagramSetup> UpdateMainDiagramSetupAsync(PortalMainDiagramSetup mainDiagramSetup);

    Task<PagedResult<PortalBudgetPosition>> GetPositionsAsync(PaginationRequest paginationRequest);
    Task<PortalBudgetPosition> CreatePositionAsync(CreatePositionRequest request);
    Task<PortalBudgetPosition> UpdatePositionAsync(UpdatePositionRequest request);
    Task<Guid> DeletePositionAsync(Guid id);

    Task<List<PortalBudgetReview>> GetBudgetReviewsByDateRangeAsync(DateOnly startDate, DateOnly endDate);
    Task<PagedResult<PortalBudgetReview>> GetBudgetReviewsAsync(PaginationRequest paginationRequest);
    Task<PortalBudgetReview> GetBudgetReviewTemplateAsync(DateOnly? date);
    Task<PortalBudgetReview> PublishBudgetReviewAsync(PublishBudgetRequestRequest request);
    Task<Guid> DeleteBudgetReviewAsync(Guid id);

    Task<PagedResult<PortalBudgetGroup>> GetBudgetGroupsAsync(PaginationRequest paginationRequest);
    Task<PortalBudgetGroup> CreateBudgetGroupAsync(CreateBudgetGroupRequest request);
    Task<PortalBudgetGroup> UpdateBudgetGroupAsync(UpdateBudgetGroupRequest request);
    Task<Guid> DeleteBudgetGroupAsync(Guid id);

    Task<PagedResult<PortalRegularExpense>> GetRegularExpensesAsync(PaginationRequest paginationRequest);
    Task<PortalRegularExpense> CreateRegularExpenseAsync(CreateRegularExpenseRequest request);
    Task<PortalRegularExpense> UpdateRegularExpenseAsync(UpdateRegularExpenseRequest request);
    Task<Guid> DeleteRegularExpenseAsync(Guid id);

    Task<List<PortalRegularExpenseReview>> GetRegularExpenseReviewsByDateRangeAsync(
        DateOnly startMonth,
        DateOnly endMonth);
    Task<PagedResult<PortalRegularExpenseReview>> GetRegularExpenseReviewsAsync(
        PaginationRequest paginationRequest);
    Task<PortalRegularExpenseReview> GetRegularExpenseReviewTemplateAsync(DateOnly? month);
    Task<PortalRegularExpenseReview> PublishRegularExpenseReviewAsync(
        PublishRegularExpenseReviewRequest request);
    Task<Guid> DeleteRegularExpenseReviewAsync(Guid id);

    Task<PortalRegularExpenseDiagramSetup> GetRegularExpenseDiagramSetupAsync();
    Task<PortalRegularExpenseDiagramSetup> UpdateRegularExpenseDiagramSetupAsync(
        UpdateRegularExpenseDiagramSetupRequest request);
}
