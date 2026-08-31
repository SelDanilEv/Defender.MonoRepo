using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;
using FluentValidation;
using MediatR;

namespace Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Commands;

public record PublishRegularExpenseReviewCommand : PublishRegularExpenseReviewRequest, IRequest<PortalRegularExpenseReview>;

public sealed class PublishRegularExpenseReviewCommandValidator
    : AbstractValidator<PublishRegularExpenseReviewCommand>
{
}

public sealed class PublishRegularExpenseReviewCommandHandler(
    IBudgetTrackerWrapper budgetTrackerWrapper)
    : IRequestHandler<PublishRegularExpenseReviewCommand, PortalRegularExpenseReview>
{
    public Task<PortalRegularExpenseReview> Handle(
        PublishRegularExpenseReviewCommand request,
        CancellationToken cancellationToken)
    {
        return budgetTrackerWrapper.PublishRegularExpenseReviewAsync(request);
    }
}
