using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using FluentValidation;
using MediatR;

namespace Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Queries;

public record GetRegularExpenseReviewsByDateRangeQuery : IRequest<List<PortalRegularExpenseReview>>
{
    public DateOnly StartMonth { get; set; } = DateOnly.MinValue;
    public DateOnly EndMonth { get; set; } = DateOnly.MaxValue;
}

public sealed class GetRegularExpenseReviewsByDateRangeQueryValidator
    : AbstractValidator<GetRegularExpenseReviewsByDateRangeQuery>
{
}

public sealed class GetRegularExpenseReviewsByDateRangeQueryHandler(
    IBudgetTrackerWrapper budgetTrackerWrapper)
    : IRequestHandler<GetRegularExpenseReviewsByDateRangeQuery, List<PortalRegularExpenseReview>>
{
    public Task<List<PortalRegularExpenseReview>> Handle(
        GetRegularExpenseReviewsByDateRangeQuery request,
        CancellationToken cancellationToken)
    {
        return budgetTrackerWrapper.GetRegularExpenseReviewsByDateRangeAsync(
            request.StartMonth,
            request.EndMonth);
    }
}
