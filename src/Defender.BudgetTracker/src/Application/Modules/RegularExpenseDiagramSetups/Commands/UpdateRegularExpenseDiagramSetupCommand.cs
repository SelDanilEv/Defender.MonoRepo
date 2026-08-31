using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Models.RegularExpenseDiagramSetups;
using Defender.BudgetTracker.Domain.Entities.DiagramSetup;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.Errors;
using Defender.Common.Extension;
using FluentValidation;
using MediatR;

namespace Defender.BudgetTracker.Application.Modules.RegularExpenseDiagramSetups.Commands;

public record UpdateRegularExpenseDiagramSetupCommand
    : UpdateRegularExpenseDiagramSetupRequest, IRequest<RegularExpenseDiagramSetup>
{
}

public sealed class UpdateRegularExpenseDiagramSetupCommandValidator
    : AbstractValidator<UpdateRegularExpenseDiagramSetupCommand>
{
    public UpdateRegularExpenseDiagramSetupCommandValidator()
    {
        RuleFor(x => x.MainCurrency)
            .Must(currency => currency != Currency.Unknown && Enum.IsDefined(currency))
            .WithMessage(ErrorCode.VL_BTS_InvalidCurrency);

        RuleFor(x => x.LastMonths)
            .GreaterThan(0)
            .When(x => x.LastMonths.HasValue)
            .WithMessage(ErrorCode.VL_BTS_NumberOfLatestMonthsMustBePositive);
    }
}

public sealed class UpdateRegularExpenseDiagramSetupCommandHandler(
    IRegularExpenseDiagramSetupService regularExpenseDiagramSetupService)
    : IRequestHandler<UpdateRegularExpenseDiagramSetupCommand, RegularExpenseDiagramSetup>
{
    public Task<RegularExpenseDiagramSetup> Handle(
        UpdateRegularExpenseDiagramSetupCommand request,
        CancellationToken cancellationToken)
    {
        return regularExpenseDiagramSetupService.UpdateRegularExpenseDiagramSetupAsync(request);
    }
}
