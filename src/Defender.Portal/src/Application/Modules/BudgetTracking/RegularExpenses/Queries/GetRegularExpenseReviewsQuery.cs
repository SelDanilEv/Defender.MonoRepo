using Defender.Common.DB.Pagination;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using FluentValidation;
using MediatR;

namespace Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Queries;

public record GetRegularExpenseReviewsQuery
    : PaginationRequest, IRequest<PagedResult<PortalRegularExpenseReview>>;

public sealed class GetRegularExpenseReviewsQueryValidator : AbstractValidator<GetRegularExpenseReviewsQuery>
{
}

public sealed class GetRegularExpenseReviewsQueryHandler(
    IBudgetTrackerWrapper budgetTrackerWrapper)
    : IRequestHandler<GetRegularExpenseReviewsQuery, PagedResult<PortalRegularExpenseReview>>
{
    public Task<PagedResult<PortalRegularExpenseReview>> Handle(
        GetRegularExpenseReviewsQuery request,
        CancellationToken cancellationToken)
    {
        return budgetTrackerWrapper.GetRegularExpenseReviewsAsync(request);
    }
}
