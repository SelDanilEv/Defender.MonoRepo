using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;
using FluentValidation;
using MediatR;

namespace Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Commands;

public record UpdateRegularExpenseDiagramSetupCommand
    : UpdateRegularExpenseDiagramSetupRequest, IRequest<PortalRegularExpenseDiagramSetup>;

public sealed class UpdateRegularExpenseDiagramSetupCommandValidator
    : AbstractValidator<UpdateRegularExpenseDiagramSetupCommand>
{
}

public sealed class UpdateRegularExpenseDiagramSetupCommandHandler(
    IBudgetTrackerWrapper budgetTrackerWrapper)
    : IRequestHandler<UpdateRegularExpenseDiagramSetupCommand, PortalRegularExpenseDiagramSetup>
{
    public Task<PortalRegularExpenseDiagramSetup> Handle(
        UpdateRegularExpenseDiagramSetupCommand request,
        CancellationToken cancellationToken)
    {
        return budgetTrackerWrapper.UpdateRegularExpenseDiagramSetupAsync(request);
    }
}
