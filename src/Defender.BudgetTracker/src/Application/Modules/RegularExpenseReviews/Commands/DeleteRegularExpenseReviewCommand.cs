using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.Common.Errors;
using Defender.Common.Extension;
using FluentValidation;
using MediatR;

namespace Defender.BudgetTracker.Application.Modules.RegularExpenseReviews.Commands;

public record DeleteRegularExpenseReviewCommand : IRequest<Guid>
{
    public Guid Id { get; init; }
}

public sealed class DeleteRegularExpenseReviewCommandValidator : AbstractValidator<DeleteRegularExpenseReviewCommand>
{
    public DeleteRegularExpenseReviewCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(ErrorCode.VL_InvalidRequest);
    }
}

public sealed class DeleteRegularExpenseReviewCommandHandler(
    IRegularExpenseReviewService regularExpenseReviewService)
    : IRequestHandler<DeleteRegularExpenseReviewCommand, Guid>
{
    public Task<Guid> Handle(
        DeleteRegularExpenseReviewCommand request,
        CancellationToken cancellationToken)
    {
        return regularExpenseReviewService.DeleteRegularExpenseReviewAsync(request.Id);
    }
}
