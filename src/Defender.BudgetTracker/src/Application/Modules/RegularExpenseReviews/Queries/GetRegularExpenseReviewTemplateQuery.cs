using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using FluentValidation;
using MediatR;

namespace Defender.BudgetTracker.Application.Modules.RegularExpenseReviews.Queries;

public record GetRegularExpenseReviewTemplateQuery : IRequest<RegularExpenseReview>
{
    public DateOnly? Month { get; set; }
}

public sealed class GetRegularExpenseReviewTemplateQueryValidator
    : AbstractValidator<GetRegularExpenseReviewTemplateQuery>
{
}

public sealed class GetRegularExpenseReviewTemplateQueryHandler(
    IRegularExpenseReviewService regularExpenseReviewService)
    : IRequestHandler<GetRegularExpenseReviewTemplateQuery, RegularExpenseReview>
{
    public Task<RegularExpenseReview> Handle(
        GetRegularExpenseReviewTemplateQuery request,
        CancellationToken cancellationToken)
    {
        return regularExpenseReviewService.GetRegularExpenseReviewTemplateAsync(request.Month);
    }
}
