using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Models.RegularExpenseReviews;
using Defender.BudgetTracker.Application.Services;
using Defender.BudgetTracker.Domain.Entities.Rates;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.DB.Pagination;
using Defender.Common.Interfaces;
using Moq;

namespace Defender.BudgetTracker.Tests.Services;

public class RegularExpenseReviewServiceTests
{
    private readonly Mock<IRegularExpenseReviewRepository> _reviewRepository = new();
    private readonly Mock<IRegularExpenseService> _expenseService = new();
    private readonly Mock<ICurrentAccountAccessor> _currentAccountAccessor = new();
    private readonly Mock<IRatesModelService> _ratesModelService = new();

    private RegularExpenseReviewService CreateSut()
        => new(
            _reviewRepository.Object,
            _expenseService.Object,
            _currentAccountAccessor.Object,
            _ratesModelService.Object);

    [Fact]
    public async Task GetTemplateAsync_WhenLatestReviewExists_CarriesAmountByIdAndUsesCurrentMetadata()
    {
        var userId = Guid.NewGuid();
        var rentId = Guid.NewGuid();
        var streamingId = Guid.NewGuid();
        var month = new DateOnly(2026, 8, 27);
        var definitions = new PagedResult<RegularExpense>
        {
            Items =
            [
                new RegularExpense
                {
                    Id = rentId,
                    UserId = userId,
                    Name = "Rent now",
                    Type = RegularExpenseType.Regular,
                    Currency = Currency.PLN,
                    DefaultAmount = 2500,
                    OrderPriority = 2
                },
                new RegularExpense
                {
                    Id = streamingId,
                    UserId = userId,
                    Name = "Streaming",
                    Type = RegularExpenseType.Subscription,
                    Currency = Currency.USD,
                    DefaultAmount = 20,
                    OrderPriority = 1
                }
            ]
        };
        var latest = new RegularExpenseReview
        {
            UserId = userId,
            Month = new DateOnly(2026, 7, 1),
            Expenses =
            [
                new ReviewedRegularExpense
                {
                    RegularExpenseId = rentId,
                    Name = "Old rent name",
                    Type = RegularExpenseType.Annual,
                    Currency = Currency.EUR,
                    Amount = 2300,
                    OrderPriority = 99
                }
            ]
        };
        var rates = new RatesModel { Date = new DateOnly(2026, 8, 1), BaseCurrency = Currency.PLN };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _expenseService.Setup(x => x.GetCurrentUserRegularExpensesAsync(It.IsAny<PaginationRequest>()))
            .ReturnsAsync(definitions);
        _reviewRepository.Setup(x => x.GetLatestRegularExpenseReviewAsync(userId)).ReturnsAsync(latest);
        _ratesModelService.Setup(x => x.GetRatesModelAsync(new DateOnly(2026, 8, 1))).ReturnsAsync(rates);

        var result = await CreateSut().GetRegularExpenseReviewTemplateAsync(month);

        Assert.Equal(new DateOnly(2026, 8, 1), result.Month);
        Assert.Equal(2300, result.Expenses.Single(x => x.RegularExpenseId == rentId).Amount);
        Assert.Equal(RegularExpenseType.Regular, result.Expenses.Single(x => x.RegularExpenseId == rentId).Type);
        Assert.Equal("Rent now", result.Expenses.Single(x => x.RegularExpenseId == rentId).Name);
        Assert.Equal(20, result.Expenses.Single(x => x.RegularExpenseId == streamingId).Amount);
        Assert.Same(rates, result.RatesModel);
    }

    [Fact]
    public async Task PublishAsync_WhenSameMonthExists_ReplacesSnapshotAndPreservesCapturedRates()
    {
        var userId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var month = new DateOnly(2026, 8, 27);
        var existingRates = new RatesModel { Date = new DateOnly(2026, 8, 1), BaseCurrency = Currency.EUR };
        var existing = new RegularExpenseReview
        {
            Id = reviewId,
            UserId = userId,
            Month = new DateOnly(2026, 8, 1),
            RatesModel = existingRates,
            Expenses =
            [
                new ReviewedRegularExpense
                {
                    RegularExpenseId = expenseId,
                    Name = "Old",
                    Type = RegularExpenseType.Regular,
                    Currency = Currency.USD,
                    Amount = 100,
                    OrderPriority = 1
                }
            ]
        };
        var definitions = new PagedResult<RegularExpense>
        {
            Items =
            [
                new RegularExpense
                {
                    Id = expenseId,
                    UserId = userId,
                    Name = "Current",
                    Type = RegularExpenseType.Annual,
                    Currency = Currency.PLN,
                    DefaultAmount = 1200,
                    OrderPriority = 4
                }
            ]
        };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _reviewRepository.Setup(x => x.GetRegularExpenseReviewAsync(userId, new DateOnly(2026, 8, 1)))
            .ReturnsAsync(existing);
        _expenseService.Setup(x => x.GetCurrentUserRegularExpensesAsync(It.IsAny<PaginationRequest>()))
            .ReturnsAsync(definitions);
        _reviewRepository.Setup(x => x.UpsertRegularExpenseReviewAsync(It.IsAny<RegularExpenseReview>()))
            .ReturnsAsync((RegularExpenseReview review) => review);

        var result = await CreateSut().PublishRegularExpenseReviewAsync(new PublishRegularExpenseReviewRequest
        {
            Month = month,
            Expenses = [new RegularExpenseReviewItemRequest { RegularExpenseId = expenseId, Amount = 1500 }]
        });

        Assert.Equal(reviewId, result.Id);
        Assert.Equal(new DateOnly(2026, 8, 1), result.Month);
        Assert.Same(existingRates, result.RatesModel);
        var snapshot = Assert.Single(result.Expenses);
        Assert.Equal("Current", snapshot.Name);
        Assert.Equal(RegularExpenseType.Annual, snapshot.Type);
        Assert.Equal(Currency.PLN, snapshot.Currency);
        Assert.Equal(1500, snapshot.Amount);
        _ratesModelService.Verify(x => x.GetRatesModelAsync(It.IsAny<DateOnly>()), Times.Never);
        _reviewRepository.Verify(x => x.UpsertRegularExpenseReviewAsync(It.Is<RegularExpenseReview>(review =>
            review.UserId == userId && review.Id == reviewId && review.Month == new DateOnly(2026, 8, 1))), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenTemplateWasDeleted_PreservesPersistedSnapshotMetadataAndAppliesSubmittedAmount()
    {
        var userId = Guid.NewGuid();
        var deletedExpenseId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var month = new DateOnly(2026, 8, 1);
        var existing = new RegularExpenseReview
        {
            Id = reviewId,
            UserId = userId,
            Month = month,
            Expenses =
            [
                new ReviewedRegularExpense
                {
                    RegularExpenseId = deletedExpenseId,
                    Name = "Historical rent",
                    Type = RegularExpenseType.Annual,
                    Currency = Currency.EUR,
                    Amount = 1200,
                    OrderPriority = 7
                }
            ]
        };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _reviewRepository.Setup(x => x.GetRegularExpenseReviewAsync(userId, month)).ReturnsAsync(existing);
        _expenseService.Setup(x => x.GetCurrentUserRegularExpensesAsync(It.IsAny<PaginationRequest>()))
            .ReturnsAsync(new PagedResult<RegularExpense>());
        _reviewRepository.Setup(x => x.UpsertRegularExpenseReviewAsync(It.IsAny<RegularExpenseReview>()))
            .ReturnsAsync((RegularExpenseReview review) => review);

        var result = await CreateSut().PublishRegularExpenseReviewAsync(new PublishRegularExpenseReviewRequest
        {
            Month = month,
            Expenses =
            [
                new RegularExpenseReviewItemRequest
                {
                    RegularExpenseId = deletedExpenseId,
                    Amount = 1500
                }
            ]
        });

        var snapshot = Assert.Single(result.Expenses);
        Assert.Equal(deletedExpenseId, snapshot.RegularExpenseId);
        Assert.Equal("Historical rent", snapshot.Name);
        Assert.Equal(RegularExpenseType.Annual, snapshot.Type);
        Assert.Equal(Currency.EUR, snapshot.Currency);
        Assert.Equal(7, snapshot.OrderPriority);
        Assert.Equal(1500, snapshot.Amount);
    }

    [Fact]
    public async Task PublishAsync_WhenForeignReviewIdProvided_DoesNotUseForeignReview()
    {
        var userId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var foreignReviewId = Guid.NewGuid();
        var month = new DateOnly(2026, 8, 1);
        var rates = new RatesModel { Date = month, BaseCurrency = Currency.PLN };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _reviewRepository.Setup(x => x.GetRegularExpenseReviewAsync(userId, month)).ReturnsAsync((RegularExpenseReview?)null);
        _reviewRepository.Setup(x => x.GetRegularExpenseReviewAsync(userId, foreignReviewId)).ReturnsAsync((RegularExpenseReview?)null);
        _expenseService.Setup(x => x.GetCurrentUserRegularExpensesAsync(It.IsAny<PaginationRequest>()))
            .ReturnsAsync(new PagedResult<RegularExpense>
            {
                Items = [new RegularExpense { Id = expenseId, UserId = userId, Name = "Owned", Currency = Currency.PLN, Type = RegularExpenseType.Regular }]
            });
        _ratesModelService.Setup(x => x.GetRatesModelAsync(month)).ReturnsAsync(rates);
        _reviewRepository.Setup(x => x.UpsertRegularExpenseReviewAsync(It.IsAny<RegularExpenseReview>()))
            .ReturnsAsync((RegularExpenseReview review) => review);

        var result = await CreateSut().PublishRegularExpenseReviewAsync(new PublishRegularExpenseReviewRequest
        {
            Id = foreignReviewId,
            Month = month,
            Expenses = [new RegularExpenseReviewItemRequest { RegularExpenseId = expenseId, Amount = 10 }]
        });

        Assert.NotEqual(foreignReviewId, result.Id);
        _reviewRepository.Verify(x => x.GetRegularExpenseReviewAsync(userId, foreignReviewId), Times.Once);
    }

    [Fact]
    public async Task GetReviewsByDateRangeAsync_WhenCalled_NormalizesRangeAndScopesToCurrentUser()
    {
        var userId = Guid.NewGuid();
        var expected = new List<RegularExpenseReview> { new() { UserId = userId } };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _reviewRepository.Setup(x => x.GetRegularExpenseReviewsAsync(
                userId,
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 8, 1)))
            .ReturnsAsync(expected);

        var result = await CreateSut().GetCurrentUserRegularExpenseReviewsAsync(
            new DateOnly(2026, 7, 27),
            new DateOnly(2026, 8, 27));

        Assert.Same(expected, result);
        _reviewRepository.Verify(x => x.GetRegularExpenseReviewsAsync(
            userId,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 8, 1)), Times.Once);
    }

    [Fact]
    public async Task DeleteRegularExpenseReviewAsync_WhenCalled_ScopesMutationToCurrentUser()
    {
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);

        var result = await CreateSut().DeleteRegularExpenseReviewAsync(reviewId);

        Assert.Equal(reviewId, result);
        _reviewRepository.Verify(x => x.DeleteRegularExpenseReviewAsync(reviewId, userId), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenNewMonth_CapturesRatesAndUsesDefinitionDefaults()
    {
        var userId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var month = new DateOnly(2026, 9, 27);
        var rates = new RatesModel { Date = new DateOnly(2026, 9, 1), BaseCurrency = Currency.USD };
        var definition = new RegularExpense
        {
            Id = expenseId,
            UserId = userId,
            Name = "Internet",
            Type = RegularExpenseType.Subscription,
            Currency = Currency.PLN,
            DefaultAmount = 80,
            OrderPriority = 2
        };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _reviewRepository.Setup(x => x.GetRegularExpenseReviewAsync(userId, new DateOnly(2026, 9, 1)))
            .ReturnsAsync((RegularExpenseReview?)null);
        _expenseService.Setup(x => x.GetCurrentUserRegularExpensesAsync(It.IsAny<PaginationRequest>()))
            .ReturnsAsync(new PagedResult<RegularExpense> { Items = [definition] });
        _ratesModelService.Setup(x => x.GetRatesModelAsync(new DateOnly(2026, 9, 1))).ReturnsAsync(rates);
        _reviewRepository.Setup(x => x.UpsertRegularExpenseReviewAsync(It.IsAny<RegularExpenseReview>()))
            .ReturnsAsync((RegularExpenseReview review) => review);

        var result = await CreateSut().PublishRegularExpenseReviewAsync(new PublishRegularExpenseReviewRequest
        {
            Month = month,
            Expenses = []
        });

        var snapshot = Assert.Single(result.Expenses);
        Assert.Equal(80, snapshot.Amount);
        Assert.Same(rates, result.RatesModel);
        definition.Name = "Changed after publish";
        definition.DefaultAmount = 999;
        Assert.Equal("Internet", snapshot.Name);
        Assert.Equal(80, snapshot.Amount);
    }

    [Fact]
    public async Task GetReviewsByDateRangeAsync_WhenReversed_ThrowsWithoutRepositoryCall()
    {
        var userId = Guid.NewGuid();
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateSut().GetCurrentUserRegularExpenseReviewsAsync(
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 8, 1)));

        _reviewRepository.Verify(x => x.GetRegularExpenseReviewsAsync(
            It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()), Times.Never);
    }
}
