using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Models.RegularExpenseReviews;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.Errors;
using Defender.Common.Extension;
using FluentValidation;
using MediatR;

namespace Defender.BudgetTracker.Application.Modules.RegularExpenseReviews.Commands;

public record PublishRegularExpenseReviewCommand : PublishRegularExpenseReviewRequest, IRequest<RegularExpenseReview>
{
}

public sealed class PublishRegularExpenseReviewCommandValidator
    : AbstractValidator<PublishRegularExpenseReviewCommand>
{
    public PublishRegularExpenseReviewCommandValidator()
    {
        RuleFor(x => x.Expenses)
            .NotNull()
            .WithMessage(ErrorCode.VL_InvalidRequest);

        RuleForEach(x => x.Expenses)
            .NotNull()
            .WithMessage(ErrorCode.VL_InvalidRequest)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.RegularExpenseId)
                    .NotEmpty()
                    .WithMessage(ErrorCode.VL_InvalidRequest);

                item.RuleFor(x => x.Amount)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(ErrorCode.VL_InvalidRequest);

                item.RuleFor(x => x.Type)
                    .Must(type => !type.HasValue ||
                        (type.Value != RegularExpenseType.Unknown && Enum.IsDefined(type.Value)))
                    .WithMessage(ErrorCode.VL_InvalidRequest);

                item.RuleFor(x => x.Currency)
                    .Must(currency => !currency.HasValue ||
                        (currency.Value != Currency.Unknown && Enum.IsDefined(currency.Value)))
                    .WithMessage(ErrorCode.VL_BTS_InvalidCurrency);
            });
    }
}

public sealed class PublishRegularExpenseReviewCommandHandler(
    IRegularExpenseReviewService regularExpenseReviewService)
    : IRequestHandler<PublishRegularExpenseReviewCommand, RegularExpenseReview>
{
    public Task<RegularExpenseReview> Handle(
        PublishRegularExpenseReviewCommand request,
        CancellationToken cancellationToken)
    {
        return regularExpenseReviewService.PublishRegularExpenseReviewAsync(request);
    }
}
