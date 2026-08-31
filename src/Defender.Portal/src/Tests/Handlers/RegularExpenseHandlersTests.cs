using Defender.Common.DB.Pagination;
using Defender.Portal.Application.Common.Interfaces.Wrappers;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using Defender.Portal.Application.Enums;
using Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;
using Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Commands;
using Defender.Portal.Application.Modules.BudgetTracking.RegularExpenses.Queries;

namespace Defender.Portal.Tests.Handlers;

public class RegularExpenseHandlersTests
{
    [Fact]
    public async Task GetRegularExpenses_WhenHandled_DelegatesPaginationToWrapper()
    {
        var wrapper = new Mock<IBudgetTrackerWrapper>();
        var request = new GetRegularExpensesQuery { Page = 2, PageSize = 15 };
        var expected = new PagedResult<PortalRegularExpense>();
        wrapper.Setup(item => item.GetRegularExpensesAsync(request)).ReturnsAsync(expected);

        var result = await new GetRegularExpensesQueryHandler(wrapper.Object)
            .Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        wrapper.Verify(item => item.GetRegularExpensesAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreateRegularExpense_WhenHandled_DelegatesIndependentRequestToWrapper()
    {
        var wrapper = new Mock<IBudgetTrackerWrapper>();
        var request = new CreateRegularExpenseCommand
        {
            Name = "Rent",
            Type = RegularExpenseType.Regular,
            Currency = Currency.PLN,
            DefaultAmount = 250_00,
            OrderPriority = 3,
        };
        var expected = new PortalRegularExpense { Id = Guid.NewGuid(), Name = request.Name };
        wrapper.Setup(item => item.CreateRegularExpenseAsync(request)).ReturnsAsync(expected);

        var result = await new CreateRegularExpenseCommandHandler(wrapper.Object)
            .Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        wrapper.Verify(item => item.CreateRegularExpenseAsync(request), Times.Once);
    }

    [Fact]
    public async Task PublishRegularExpenseReview_WhenHandled_DelegatesMonthAndSnapshotsToWrapper()
    {
        var wrapper = new Mock<IBudgetTrackerWrapper>();
        var request = new PublishRegularExpenseReviewCommand
        {
            Month = new DateOnly(2026, 8, 19),
            Expenses =
            [
                new RegularExpenseReviewItemRequest
                {
                    RegularExpenseId = Guid.NewGuid(),
                    Amount = 1_200,
                },
            ],
        };
        var expected = new PortalRegularExpenseReview { Month = request.Month };
        wrapper.Setup(item => item.PublishRegularExpenseReviewAsync(request)).ReturnsAsync(expected);

        var result = await new PublishRegularExpenseReviewCommandHandler(wrapper.Object)
            .Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        wrapper.Verify(item => item.PublishRegularExpenseReviewAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetRegularExpenseReviewsByDateRange_WhenHandled_ForwardsBothMonths()
    {
        var wrapper = new Mock<IBudgetTrackerWrapper>();
        var request = new GetRegularExpenseReviewsByDateRangeQuery
        {
            StartMonth = new DateOnly(2026, 1, 1),
            EndMonth = new DateOnly(2026, 3, 1),
        };
        var expected = new List<PortalRegularExpenseReview>();
        wrapper
            .Setup(item => item.GetRegularExpenseReviewsByDateRangeAsync(request.StartMonth, request.EndMonth))
            .ReturnsAsync(expected);

        var result = await new GetRegularExpenseReviewsByDateRangeQueryHandler(wrapper.Object)
            .Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        wrapper.Verify(
            item => item.GetRegularExpenseReviewsByDateRangeAsync(request.StartMonth, request.EndMonth),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRegularExpenseDiagramSetup_WhenHandled_DelegatesIndependentSetupToWrapper()
    {
        var wrapper = new Mock<IBudgetTrackerWrapper>();
        var request = new UpdateRegularExpenseDiagramSetupCommand
        {
            MainCurrency = Application.Enums.Currency.EUR,
            LastMonths = 6,
            EndMonth = new DateOnly(2026, 8, 1),
        };
        var expected = new PortalRegularExpenseDiagramSetup
        {
            MainCurrency = request.MainCurrency,
            LastMonths = request.LastMonths,
            EndMonth = request.EndMonth,
        };
        wrapper.Setup(item => item.UpdateRegularExpenseDiagramSetupAsync(request)).ReturnsAsync(expected);

        var result = await new UpdateRegularExpenseDiagramSetupCommandHandler(wrapper.Object)
            .Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        wrapper.Verify(item => item.UpdateRegularExpenseDiagramSetupAsync(request), Times.Once);
    }
}
