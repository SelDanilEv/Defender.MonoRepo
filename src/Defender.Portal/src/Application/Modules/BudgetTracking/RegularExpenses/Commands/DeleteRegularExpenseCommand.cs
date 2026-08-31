using Defender.Portal.Application.Common.Interfaces.Wrappers;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Commands;

public record DeleteRegularExpenseCommand : IRequest<Guid>
{
    [FromRoute]
    public Guid Id { get; init; }
}

public sealed class DeleteRegularExpenseCommandValidator : AbstractValidator<DeleteRegularExpenseCommand>
{
}

public sealed class DeleteRegularExpenseCommandHandler(
    IBudgetTrackerWrapper budgetTrackerWrapper)
    : IRequestHandler<DeleteRegularExpenseCommand, Guid>
{
    public Task<Guid> Handle(
        DeleteRegularExpenseCommand request,
        CancellationToken cancellationToken)
    {
        return budgetTrackerWrapper.DeleteRegularExpenseAsync(request.Id);
    }
}
