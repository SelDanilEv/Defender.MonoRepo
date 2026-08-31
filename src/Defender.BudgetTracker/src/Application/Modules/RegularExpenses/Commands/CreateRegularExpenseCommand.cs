using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Models.RegularExpenses;
using Defender.BudgetTracker.Application.Modules.RegularExpenses;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.Errors;
using Defender.Common.Extension;
using FluentValidation;
using MediatR;

namespace Defender.BudgetTracker.Application.Modules.RegularExpenses.Commands;

public record CreateRegularExpenseCommand : CreateRegularExpenseRequest, IRequest<RegularExpense>
{
}

public sealed class CreateRegularExpenseCommandValidator : AbstractValidator<CreateRegularExpenseCommand>
{
    public CreateRegularExpenseCommandValidator()
    {
        RuleFor(x => x.Name)
            .Must(IsValidName)
            .WithMessage(ErrorCode.VL_BTS_InvalidPositionName);

        RuleFor(x => x.Type)
            .Must(IsSupportedType)
            .WithMessage(ErrorCode.VL_InvalidRequest);

        RuleFor(x => x.Currency)
            .Must(IsSupportedCurrency)
            .WithMessage(ErrorCode.VL_BTS_InvalidCurrency);

        RuleFor(x => x.DefaultAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage(ErrorCode.VL_InvalidRequest);
    }

    internal static bool IsValidName(string? name)
        => !string.IsNullOrWhiteSpace(name)
            && name.Trim().Length <= RegularExpenseValidationConstants.MaxNameLength;

    internal static bool IsSupportedType(RegularExpenseType type)
        => type != RegularExpenseType.Unknown && Enum.IsDefined(type);

    internal static bool IsSupportedCurrency(Currency currency)
        => currency != Currency.Unknown && Enum.IsDefined(currency);
}

public sealed class CreateRegularExpenseCommandHandler(
    IRegularExpenseService regularExpenseService)
    : IRequestHandler<CreateRegularExpenseCommand, RegularExpense>
{
    public Task<RegularExpense> Handle(
        CreateRegularExpenseCommand request,
        CancellationToken cancellationToken)
    {
        return regularExpenseService.CreateRegularExpenseAsync(request);
    }
}
