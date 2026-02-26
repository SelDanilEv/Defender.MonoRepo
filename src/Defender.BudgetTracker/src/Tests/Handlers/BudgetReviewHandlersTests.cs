using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Modules.BudgetReviews.Commands;
using Defender.BudgetTracker.Application.Modules.BudgetReviews.Queries;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.Common.DB.Pagination;
using Moq;

namespace Defender.BudgetTracker.Tests.Handlers;

public class BudgetReviewHandlersTests
{
    private readonly Mock<IBudgetReviewService> _budgetReviewService = new();

    [Fact]
    public async Task GetBudgetReviewsQueryHandler_WhenCalled_DelegatesToService()
    {
        var request = new GetBudgetReviewsQuery();
        var expected = new PagedResult<BudgetReview> { Items = [new BudgetReview()] };
        _budgetReviewService.Setup(x => x.GetCurrentUserBudgetReviewsAsync(request)).ReturnsAsync(expected);

        var handler = new GetBudgetReviewsQueryHandler(_budgetReviewService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _budgetReviewService.Verify(x => x.GetCurrentUserBudgetReviewsAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetBudgetReviewsByDateRangeQueryHandler_WhenCalled_DelegatesToService()
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new GetBudgetReviewsByDateRangeQuery { StartDate = start, EndDate = end };
        var expected = new List<BudgetReview> { new() };
        _budgetReviewService.Setup(x => x.GetCurrentUserBudgetReviewsAsync(start, end)).ReturnsAsync(expected);

        var handler = new GetBudgetReviewsByDateRangeQueryHandler(_budgetReviewService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _budgetReviewService.Verify(x => x.GetCurrentUserBudgetReviewsAsync(start, end), Times.Once);
    }

    [Fact]
    public async Task GetBudgetReviewTemplateQueryHandler_WhenCalled_DelegatesToService()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new GetBudgetReviewTemplateQuery { Date = date };
        var expected = new BudgetReview { Date = date };
        _budgetReviewService.Setup(x => x.GetBudgetReviewTemplateAsync(date)).ReturnsAsync(expected);

        var handler = new GetBudgetReviewTemplateQueryHandler(_budgetReviewService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _budgetReviewService.Verify(x => x.GetBudgetReviewTemplateAsync(date), Times.Once);
    }

    [Fact]
    public async Task GetBudgetReviewTemplateQueryHandler_WhenDateNull_PassesNullToService()
    {
        var request = new GetBudgetReviewTemplateQuery { Date = null };
        var expected = new BudgetReview();
        _budgetReviewService.Setup(x => x.GetBudgetReviewTemplateAsync(null)).ReturnsAsync(expected);

        var handler = new GetBudgetReviewTemplateQueryHandler(_budgetReviewService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _budgetReviewService.Verify(x => x.GetBudgetReviewTemplateAsync(null), Times.Once);
    }

    [Fact]
    public async Task DeleteBudgetReviewCommandHandler_WhenCalled_DelegatesToService()
    {
        var reviewId = Guid.NewGuid();
        var request = new DeleteBudgetReviewCommand { Id = reviewId };
        _budgetReviewService.Setup(x => x.DeleteBudgetReviewAsync(reviewId)).ReturnsAsync(reviewId);

        var handler = new DeleteBudgetReviewCommandHandler(_budgetReviewService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(reviewId, result);
        _budgetReviewService.Verify(x => x.DeleteBudgetReviewAsync(reviewId), Times.Once);
    }

    [Fact]
    public async Task PublishBudgetReviewCommandHandler_WhenCalled_DelegatesToService()
    {
        var request = new PublishBudgetReviewCommand
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ReviewedPositions = [new Application.Models.BudgetReview.PositionToPublish { Name = "A", Amount = 1 }]
        };
        var expected = new BudgetReview();
        _budgetReviewService.Setup(x => x.PublishBudgetReviewAsync(request)).ReturnsAsync(expected);

        var handler = new PublishBudgetReviewCommandHandler(_budgetReviewService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _budgetReviewService.Verify(x => x.PublishBudgetReviewAsync(request), Times.Once);
    }
}
