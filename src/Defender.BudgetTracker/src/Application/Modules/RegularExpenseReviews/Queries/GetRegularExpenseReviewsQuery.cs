using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.Common.DB.Pagination;
using FluentValidation;
using MediatR;

namespace Defender.BudgetTracker.Application.Modules.RegularExpenseReviews.Queries;

public record GetRegularExpenseReviewsQuery : PaginationRequest, IRequest<PagedResult<RegularExpenseReview>>
{
}

public sealed class GetRegularExpenseReviewsQueryValidator : AbstractValidator<GetRegularExpenseReviewsQuery>
{
}

public sealed class GetRegularExpenseReviewsQueryHandler(
    IRegularExpenseReviewService regularExpenseReviewService)
    : IRequestHandler<GetRegularExpenseReviewsQuery, PagedResult<RegularExpenseReview>>
{
    public Task<PagedResult<RegularExpenseReview>> Handle(
        GetRegularExpenseReviewsQuery request,
        CancellationToken cancellationToken)
    {
        return regularExpenseReviewService.GetCurrentUserRegularExpenseReviewsAsync(request);
    }
}
