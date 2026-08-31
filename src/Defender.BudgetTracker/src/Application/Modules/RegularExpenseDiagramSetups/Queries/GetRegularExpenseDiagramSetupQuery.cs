using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Domain.Entities.DiagramSetup;
using FluentValidation;
using MediatR;

namespace Defender.BudgetTracker.Application.Modules.RegularExpenseDiagramSetups.Queries;

public record GetRegularExpenseDiagramSetupQuery : IRequest<RegularExpenseDiagramSetup>
{
}

public sealed class GetRegularExpenseDiagramSetupQueryValidator
    : AbstractValidator<GetRegularExpenseDiagramSetupQuery>
{
}

public sealed class GetRegularExpenseDiagramSetupQueryHandler(
    IRegularExpenseDiagramSetupService regularExpenseDiagramSetupService)
    : IRequestHandler<GetRegularExpenseDiagramSetupQuery, RegularExpenseDiagramSetup>
{
    public Task<RegularExpenseDiagramSetup> Handle(
        GetRegularExpenseDiagramSetupQuery request,
        CancellationToken cancellationToken)
    {
        return regularExpenseDiagramSetupService.GetCurrentUserRegularExpenseDiagramSetupAsync();
    }
}
