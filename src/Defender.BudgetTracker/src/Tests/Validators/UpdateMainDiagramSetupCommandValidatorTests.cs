using Defender.BudgetTracker.Application.Modules.DiagramSetups.Commands;
using FluentValidation.TestHelper;

namespace Defender.BudgetTracker.Tests.Validators;

public class UpdateMainDiagramSetupCommandValidatorTests
{
    private readonly UpdateMainDiagramSetupCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenLastMonthsZero_HasValidationError()
    {
        var command = new UpdateMainDiagramSetupCommand { LastMonths = 0 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.LastMonths);
    }

    [Fact]
    public void Validate_WhenLastMonthsNegative_HasValidationError()
    {
        var command = new UpdateMainDiagramSetupCommand { LastMonths = -1 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.LastMonths);
    }

    [Fact]
    public void Validate_WhenLastMonthsPositive_HasNoErrors()
    {
        var command = new UpdateMainDiagramSetupCommand { LastMonths = 12 };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
