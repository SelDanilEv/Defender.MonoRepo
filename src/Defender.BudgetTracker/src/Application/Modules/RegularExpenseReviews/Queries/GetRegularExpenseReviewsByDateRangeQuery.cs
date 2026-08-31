using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.Common.Errors;
using FluentValidation;
using MediatR;

namespace Defender.BudgetTracker.Application.Modules.RegularExpenseReviews.Queries;

public record GetRegularExpenseReviewsByDateRangeQuery : IRequest<List<RegularExpenseReview>>
{
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
}

public sealed class GetRegularExpenseReviewsByDateRangeQueryValidator
    : AbstractValidator<GetRegularExpenseReviewsByDateRangeQuery>
{
    public GetRegularExpenseReviewsByDateRangeQueryValidator()
    {
        RuleFor(x => x.EndMonth)
            .Must((request, endMonth) => RegularExpenseReview.NormalizeMonth(request.StartMonth)
                <= RegularExpenseReview.NormalizeMonth(endMonth))
            .WithMessage(ErrorCodeHelper.GetErrorCode(ErrorCode.VL_InvalidRequest));
    }
}

public sealed class GetRegularExpenseReviewsByDateRangeQueryHandler(
    IRegularExpenseReviewService regularExpenseReviewService)
    : IRequestHandler<GetRegularExpenseReviewsByDateRangeQuery, List<RegularExpenseReview>>
{
    public Task<List<RegularExpenseReview>> Handle(
        GetRegularExpenseReviewsByDateRangeQuery request,
        CancellationToken cancellationToken)
    {
        return regularExpenseReviewService.GetCurrentUserRegularExpenseReviewsAsync(
            request.StartMonth,
            request.EndMonth);
    }
}
