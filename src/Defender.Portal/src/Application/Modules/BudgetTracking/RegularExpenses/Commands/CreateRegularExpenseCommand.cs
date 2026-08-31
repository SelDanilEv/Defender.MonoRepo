using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;
using FluentValidation;
using MediatR;

namespace Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Commands;

public record CreateRegularExpenseCommand : CreateRegularExpenseRequest, IRequest<PortalRegularExpense>;

public sealed class CreateRegularExpenseCommandValidator : AbstractValidator<CreateRegularExpenseCommand>
{
}

public sealed class CreateRegularExpenseCommandHandler(
    IBudgetTrackerWrapper budgetTrackerWrapper)
    : IRequestHandler<CreateRegularExpenseCommand, PortalRegularExpense>
{
    public Task<PortalRegularExpense> Handle(
        CreateRegularExpenseCommand request,
        CancellationToken cancellationToken)
    {
        return budgetTrackerWrapper.CreateRegularExpenseAsync(request);
    }
}
