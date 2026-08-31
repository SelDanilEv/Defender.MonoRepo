using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using FluentValidation;
using MediatR;

namespace Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Queries;

public record GetRegularExpenseReviewTemplateQuery : IRequest<PortalRegularExpenseReview>
{
    public DateOnly? Month { get; set; }
}

public sealed class GetRegularExpenseReviewTemplateQueryValidator
    : AbstractValidator<GetRegularExpenseReviewTemplateQuery>
{
}

public sealed class GetRegularExpenseReviewTemplateQueryHandler(
    IBudgetTrackerWrapper budgetTrackerWrapper)
    : IRequestHandler<GetRegularExpenseReviewTemplateQuery, PortalRegularExpenseReview>
{
    public Task<PortalRegularExpenseReview> Handle(
        GetRegularExpenseReviewTemplateQuery request,
        CancellationToken cancellationToken)
    {
        return budgetTrackerWrapper.GetRegularExpenseReviewTemplateAsync(request.Month);
    }
}
