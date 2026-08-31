using Defender.Portal.Application.Common.Interfaces.Wrappers;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Commands;

public record DeleteRegularExpenseReviewCommand : IRequest<Guid>
{
    [FromRoute]
    public Guid Id { get; init; }
}

public sealed class DeleteRegularExpenseReviewCommandValidator
    : AbstractValidator<DeleteRegularExpenseReviewCommand>
{
}

public sealed class DeleteRegularExpenseReviewCommandHandler(
    IBudgetTrackerWrapper budgetTrackerWrapper)
    : IRequestHandler<DeleteRegularExpenseReviewCommand, Guid>
{
    public Task<Guid> Handle(
        DeleteRegularExpenseReviewCommand request,
        CancellationToken cancellationToken)
    {
        return budgetTrackerWrapper.DeleteRegularExpenseReviewAsync(request.Id);
    }
}
