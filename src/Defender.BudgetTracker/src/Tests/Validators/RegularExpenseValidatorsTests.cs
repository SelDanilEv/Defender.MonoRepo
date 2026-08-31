using Defender.BudgetTracker.Application.Models.RegularExpenseReviews;
using Defender.BudgetTracker.Application.Modules.RegularExpenseReviews.Commands;
using Defender.BudgetTracker.Application.Modules.RegularExpenseReviews.Queries;
using Defender.BudgetTracker.Application.Modules.RegularExpenses.Commands;
using Defender.BudgetTracker.Application.Modules.RegularExpenseDiagramSetups.Commands;
using Defender.BudgetTracker.Domain.Enums;
using FluentValidation.TestHelper;

namespace Defender.BudgetTracker.Tests.Validators;

public class RegularExpenseValidatorsTests
{
    [Fact]
    public void Create_WhenNameBlank_HasValidationError()
    {
        var result = new CreateRegularExpenseCommandValidator().TestValidate(new CreateRegularExpenseCommand
        {
            Name = "   ",
            Type = RegularExpenseType.Regular,
            Currency = Currency.PLN
        });

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Create_WhenNameOverlong_HasValidationError()
    {
        var result = new CreateRegularExpenseCommandValidator().TestValidate(new CreateRegularExpenseCommand
        {
            Name = new string('x', 201),
            Type = RegularExpenseType.Regular,
            Currency = Currency.PLN
        });

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Create_WhenAmountNegative_HasValidationError()
    {
        var result = new CreateRegularExpenseCommandValidator().TestValidate(new CreateRegularExpenseCommand
        {
            Name = "Rent",
            Type = RegularExpenseType.Regular,
            Currency = Currency.PLN,
            DefaultAmount = -1
        });

        result.ShouldHaveValidationErrorFor(x => x.DefaultAmount);
    }

    [Fact]
    public void Create_WhenTypeUnknown_HasValidationError()
    {
        var result = new CreateRegularExpenseCommandValidator().TestValidate(new CreateRegularExpenseCommand
        {
            Name = "Rent",
            Type = RegularExpenseType.Unknown,
            Currency = Currency.PLN
        });

        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Create_WhenCurrencyUnknown_HasValidationError()
    {
        var result = new CreateRegularExpenseCommandValidator().TestValidate(new CreateRegularExpenseCommand
        {
            Name = "Rent",
            Type = RegularExpenseType.Regular,
            Currency = Currency.Unknown
        });

        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void Update_WhenOptionalNameBlank_HasValidationError()
    {
        var result = new UpdateRegularExpenseCommandValidator().TestValidate(new UpdateRegularExpenseCommand
        {
            Id = Guid.NewGuid(),
            Name = "  "
        });

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Publish_WhenAmountNegative_HasValidationError()
    {
        var result = new PublishRegularExpenseReviewCommandValidator().TestValidate(new PublishRegularExpenseReviewCommand
        {
            Month = new DateOnly(2026, 8, 1),
            Expenses = [new RegularExpenseReviewItemRequest { RegularExpenseId = Guid.NewGuid(), Amount = -1 }]
        });

        result.ShouldHaveValidationErrorFor("Expenses[0].Amount");
    }

    [Fact]
    public void Publish_WhenExpensesNull_HasValidationError()
    {
        var result = new PublishRegularExpenseReviewCommandValidator().TestValidate(
            new PublishRegularExpenseReviewCommand { Expenses = null! });

        result.ShouldHaveValidationErrorFor(x => x.Expenses);
    }

    [Fact]
    public void Publish_WhenExpenseItemNull_HasValidationError()
    {
        var result = new PublishRegularExpenseReviewCommandValidator().TestValidate(
            new PublishRegularExpenseReviewCommand { Expenses = [null!] });

        result.ShouldHaveValidationErrorFor("Expenses[0]");
    }

    [Fact]
    public void DateRange_WhenReversed_HasValidationError()
    {
        var result = new GetRegularExpenseReviewsByDateRangeQueryValidator().TestValidate(
            new GetRegularExpenseReviewsByDateRangeQuery
            {
                StartMonth = new DateOnly(2026, 9, 1),
                EndMonth = new DateOnly(2026, 8, 1)
            });

        result.ShouldHaveValidationErrorFor(x => x.EndMonth);
    }

    [Fact]
    public void DiagramSetup_WhenCurrencyUnknown_HasValidationError()
    {
        var result = new UpdateRegularExpenseDiagramSetupCommandValidator().TestValidate(
            new UpdateRegularExpenseDiagramSetupCommand { MainCurrency = Currency.Unknown });

        result.ShouldHaveValidationErrorFor(x => x.MainCurrency);
    }

    [Fact]
    public void DiagramSetup_WhenLastMonthsNegative_HasValidationError()
    {
        var result = new UpdateRegularExpenseDiagramSetupCommandValidator().TestValidate(
            new UpdateRegularExpenseDiagramSetupCommand
            {
                MainCurrency = Currency.PLN,
                LastMonths = -1
            });

        result.ShouldHaveValidationErrorFor(x => x.LastMonths);
    }
}
