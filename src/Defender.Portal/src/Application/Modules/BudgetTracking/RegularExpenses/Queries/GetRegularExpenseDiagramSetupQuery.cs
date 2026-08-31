using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using FluentValidation;
using MediatR;

namespace Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Queries;

public record GetRegularExpenseDiagramSetupQuery : IRequest<PortalRegularExpenseDiagramSetup>;

public sealed class GetRegularExpenseDiagramSetupQueryValidator
    : AbstractValidator<GetRegularExpenseDiagramSetupQuery>
{
}

public sealed class GetRegularExpenseDiagramSetupQueryHandler(
    IBudgetTrackerWrapper budgetTrackerWrapper)
    : IRequestHandler<GetRegularExpenseDiagramSetupQuery, PortalRegularExpenseDiagramSetup>
{
    public Task<PortalRegularExpenseDiagramSetup> Handle(
        GetRegularExpenseDiagramSetupQuery request,
        CancellationToken cancellationToken)
    {
        return budgetTrackerWrapper.GetRegularExpenseDiagramSetupAsync();
    }
}
