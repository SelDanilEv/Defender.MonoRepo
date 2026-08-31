using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Application.Models.RegularExpenses;
using Defender.BudgetTracker.Application.Services;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;
using Defender.Common.Interfaces;
using Moq;

namespace Defender.BudgetTracker.Tests.Services;

public class RegularExpenseServiceTests
{
    private readonly Mock<IRegularExpenseRepository> _repository = new();
    private readonly Mock<ICurrentAccountAccessor> _currentAccountAccessor = new();

    private RegularExpenseService CreateSut()
        => new(_repository.Object, _currentAccountAccessor.Object);

    [Fact]
    public async Task GetCurrentUserRegularExpensesAsync_WhenCalled_UsesCurrentUserId()
    {
        var userId = Guid.NewGuid();
        var pagination = new PaginationRequest();
        var expected = new PagedResult<RegularExpense> { Items = [new RegularExpense()] };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _repository.Setup(x => x.GetRegularExpensesAsync(pagination, userId)).ReturnsAsync(expected);

        var result = await CreateSut().GetCurrentUserRegularExpensesAsync(pagination);

        Assert.Same(expected, result);
        _repository.Verify(x => x.GetRegularExpensesAsync(pagination, userId), Times.Once);
    }

    [Fact]
    public async Task CreateRegularExpenseAsync_WhenCalled_AssignsCurrentUserId()
    {
        var userId = Guid.NewGuid();
        var request = new CreateRegularExpenseRequest
        {
            Name = "Rent",
            Type = RegularExpenseType.Regular,
            Currency = Currency.PLN,
            DefaultAmount = 2500,
            OrderPriority = 3
        };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _repository.Setup(x => x.CreateRegularExpenseAsync(It.IsAny<RegularExpense>()))
            .ReturnsAsync((RegularExpense expense) => expense);

        var result = await CreateSut().CreateRegularExpenseAsync(request);

        Assert.Equal(userId, result.UserId);
        Assert.Equal("Rent", result.Name);
        Assert.Equal(2500, result.DefaultAmount);
        _repository.Verify(x => x.CreateRegularExpenseAsync(It.Is<RegularExpense>(expense =>
            expense.UserId == userId && expense.Name == "Rent")), Times.Once);
    }

    [Fact]
    public async Task UpdateRegularExpenseAsync_WhenCalled_ScopesMutationToCurrentUser()
    {
        var userId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var request = new UpdateRegularExpenseRequest
        {
            Id = expenseId,
            Name = "Updated rent",
            DefaultAmount = 2600
        };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _repository.Setup(x => x.UpdateRegularExpenseAsync(
                It.IsAny<UpdateModelRequest<RegularExpense>>(), userId))
            .ReturnsAsync(new RegularExpense { Id = expenseId, UserId = userId, Name = "Updated rent" });

        var result = await CreateSut().UpdateRegularExpenseAsync(request);

        Assert.Equal(expenseId, result.Id);
        _repository.Verify(x => x.UpdateRegularExpenseAsync(
            It.Is<UpdateModelRequest<RegularExpense>>(update => update.ModelId == expenseId), userId), Times.Once);
    }

    [Fact]
    public async Task DeleteRegularExpenseAsync_WhenCalled_ScopesMutationToCurrentUser()
    {
        var userId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);

        var result = await CreateSut().DeleteRegularExpenseAsync(expenseId);

        Assert.Equal(expenseId, result);
        _repository.Verify(x => x.DeleteRegularExpenseAsync(expenseId, userId), Times.Once);
    }
}
