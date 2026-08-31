using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;
using FluentValidation;
using MediatR;

namespace Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Commands;

public record UpdateRegularExpenseCommand : UpdateRegularExpenseRequest, IRequest<PortalRegularExpense>;

public sealed class UpdateRegularExpenseCommandValidator : AbstractValidator<UpdateRegularExpenseCommand>
{
}

public sealed class UpdateRegularExpenseCommandHandler(
    IBudgetTrackerWrapper budgetTrackerWrapper)
    : IRequestHandler<UpdateRegularExpenseCommand, PortalRegularExpense>
{
    public Task<PortalRegularExpense> Handle(
        UpdateRegularExpenseCommand request,
        CancellationToken cancellationToken)
    {
        return budgetTrackerWrapper.UpdateRegularExpenseAsync(request);
    }
}
