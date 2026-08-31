using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Models.RegularExpenses;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.Errors;
using Defender.Common.Extension;
using FluentValidation;
using MediatR;

namespace Defender.BudgetTracker.Application.Modules.RegularExpenses.Commands;

public record UpdateRegularExpenseCommand : UpdateRegularExpenseRequest, IRequest<RegularExpense>
{
}

public sealed class UpdateRegularExpenseCommandValidator : AbstractValidator<UpdateRegularExpenseCommand>
{
    public UpdateRegularExpenseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(ErrorCode.VL_InvalidRequest);

        RuleFor(x => x.Name)
            .Must(name => name is null || CreateRegularExpenseCommandValidator.IsValidName(name))
            .WithMessage(ErrorCode.VL_BTS_InvalidPositionName);

        RuleFor(x => x.Type)
            .Must(type => !type.HasValue || CreateRegularExpenseCommandValidator.IsSupportedType(type.Value))
            .WithMessage(ErrorCode.VL_InvalidRequest);

        RuleFor(x => x.Currency)
            .Must(currency => !currency.HasValue || CreateRegularExpenseCommandValidator.IsSupportedCurrency(currency.Value))
            .WithMessage(ErrorCode.VL_BTS_InvalidCurrency);

        RuleFor(x => x.DefaultAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DefaultAmount.HasValue)
            .WithMessage(ErrorCode.VL_InvalidRequest);
    }
}

public sealed class UpdateRegularExpenseCommandHandler(
    IRegularExpenseService regularExpenseService)
    : IRequestHandler<UpdateRegularExpenseCommand, RegularExpense>
{
    public Task<RegularExpense> Handle(
        UpdateRegularExpenseCommand request,
        CancellationToken cancellationToken)
    {
        return regularExpenseService.UpdateRegularExpenseAsync(request);
    }
}
