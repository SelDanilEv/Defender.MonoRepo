using Defender.BudgetTracker.Application.Modules.BudgetReviews.Commands;
using FluentValidation.TestHelper;

namespace Defender.BudgetTracker.Tests.Validators;

public class PublishBudgetReviewCommandValidatorTests
{
    private readonly PublishBudgetReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenReviewedPositionsEmpty_HasValidationError()
    {
        var command = new PublishBudgetReviewCommand
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ReviewedPositions = []
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ReviewedPositions);
    }

    [Fact]
    public void Validate_WhenReviewedPositionsHasItems_HasNoErrors()
    {
        var command = new PublishBudgetReviewCommand
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ReviewedPositions = [new Application.Models.BudgetReview.PositionToPublish { Name = "A", Amount = 1 }]
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
