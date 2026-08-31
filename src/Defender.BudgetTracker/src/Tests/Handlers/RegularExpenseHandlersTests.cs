using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Models.RegularExpenses;
using Defender.BudgetTracker.Application.Models.RegularExpenseReviews;
using Defender.BudgetTracker.Application.Modules.RegularExpenseReviews.Commands;
using Defender.BudgetTracker.Application.Modules.RegularExpenseReviews.Queries;
using Defender.BudgetTracker.Application.Modules.RegularExpenses.Commands;
using Defender.BudgetTracker.Application.Modules.RegularExpenses.Queries;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.DB.Pagination;
using Moq;

namespace Defender.BudgetTracker.Tests.Handlers;

public class RegularExpenseHandlersTests
{
    [Fact]
    public async Task GetRegularExpensesQueryHandler_WhenCalled_DelegatesToService()
    {
        var service = new Mock<IRegularExpenseService>();
        var request = new GetRegularExpensesQuery();
        var expected = new PagedResult<RegularExpense> { Items = [new RegularExpense()] };
        service.Setup(x => x.GetCurrentUserRegularExpensesAsync(request)).ReturnsAsync(expected);

        var result = await new GetRegularExpensesQueryHandler(service.Object).Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        service.Verify(x => x.GetCurrentUserRegularExpensesAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreateRegularExpenseCommandHandler_WhenCalled_DelegatesToService()
    {
        var service = new Mock<IRegularExpenseService>();
        var request = new CreateRegularExpenseCommand
        {
            Name = "Rent",
            Type = RegularExpenseType.Regular,
            Currency = Currency.PLN
        };
        var expected = new RegularExpense { Name = "Rent" };
        service.Setup(x => x.CreateRegularExpenseAsync(request)).ReturnsAsync(expected);

        var result = await new CreateRegularExpenseCommandHandler(service.Object).Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        service.Verify(x => x.CreateRegularExpenseAsync(request), Times.Once);
    }

    [Fact]
    public async Task PublishRegularExpenseReviewCommandHandler_WhenCalled_DelegatesToService()
    {
        var service = new Mock<IRegularExpenseReviewService>();
        var request = new PublishRegularExpenseReviewCommand
        {
            Month = new DateOnly(2026, 8, 1),
            Expenses = [new RegularExpenseReviewItemRequest { RegularExpenseId = Guid.NewGuid(), Amount = 1 }]
        };
        var expected = new RegularExpenseReview { Month = request.Month };
        service.Setup(x => x.PublishRegularExpenseReviewAsync(request)).ReturnsAsync(expected);

        var result = await new PublishRegularExpenseReviewCommandHandler(service.Object)
            .Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        service.Verify(x => x.PublishRegularExpenseReviewAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetRegularExpenseReviewTemplateQueryHandler_WhenCalled_DelegatesToService()
    {
        var service = new Mock<IRegularExpenseReviewService>();
        var request = new GetRegularExpenseReviewTemplateQuery { Month = new DateOnly(2026, 8, 27) };
        var expected = new RegularExpenseReview { Month = new DateOnly(2026, 8, 1) };
        service.Setup(x => x.GetRegularExpenseReviewTemplateAsync(request.Month)).ReturnsAsync(expected);

        var result = await new GetRegularExpenseReviewTemplateQueryHandler(service.Object)
            .Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        service.Verify(x => x.GetRegularExpenseReviewTemplateAsync(request.Month), Times.Once);
    }
}
