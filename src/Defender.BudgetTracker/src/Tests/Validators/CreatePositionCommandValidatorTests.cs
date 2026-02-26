using Defender.BudgetTracker.Application.Modules.Positions.Commands;
using Defender.BudgetTracker.Domain.Enums;
using FluentValidation.TestHelper;

namespace Defender.BudgetTracker.Tests.Validators;

public class CreatePositionCommandValidatorTests
{
    private readonly CreatePositionCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCurrencyUnknown_HasValidationError()
    {
        var command = new CreatePositionCommand { Name = "Pos", Currency = Currency.Unknown };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Currency);
    }

    [Fact]
    public void Validate_WhenNameEmpty_HasValidationError()
    {
        var command = new CreatePositionCommand { Name = "", Currency = Currency.USD };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Validate_WhenAllValid_HasNoErrors()
    {
        var command = new CreatePositionCommand { Name = "Position 1", Currency = Currency.PLN };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
