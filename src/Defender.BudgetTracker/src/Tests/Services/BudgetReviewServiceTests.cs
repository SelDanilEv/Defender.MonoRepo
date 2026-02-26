using AutoMapper;
using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Models.BudgetReview;
using Defender.BudgetTracker.Application.Services;
using Defender.BudgetTracker.Domain.Entities.Position;
using Defender.BudgetTracker.Domain.Entities.Rates;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.DB.Pagination;
using Moq;

namespace Defender.BudgetTracker.Tests.Services;

public class BudgetReviewServiceTests
{
    private readonly Mock<IBudgetReviewRepository> _budgetReviewRepository = new();
    private readonly Mock<IPositionService> _positionService = new();
    private readonly Mock<Defender.Common.Interfaces.ICurrentAccountAccessor> _currentAccountAccessor = new();
    private readonly Mock<IRatesModelService> _ratesModelService = new();
    private readonly Mock<IMapper> _mapper = new();

    private BudgetReviewService CreateSut()
        => new(
            _budgetReviewRepository.Object,
            _positionService.Object,
            _currentAccountAccessor.Object,
            _ratesModelService.Object,
            _mapper.Object);

    [Fact]
    public async Task GetCurrentUserBudgetReviewsAsync_WithPagination_UsesCurrentUserId()
    {
        var userId = Guid.NewGuid();
        var paginationRequest = new PaginationRequest();
        var expected = new PagedResult<BudgetReview> { Items = [new BudgetReview()] };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _budgetReviewRepository
            .Setup(x => x.GetBudgetReviewsAsync(paginationRequest, userId))
            .ReturnsAsync(expected);

        var result = await CreateSut().GetCurrentUserBudgetReviewsAsync(paginationRequest);

        Assert.Same(expected, result);
        _budgetReviewRepository.Verify(x => x.GetBudgetReviewsAsync(paginationRequest, userId), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserBudgetReviewsAsync_WithDateRange_UsesCurrentUserId()
    {
        var userId = Guid.NewGuid();
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var expected = new List<BudgetReview> { new() { UserId = userId } };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _budgetReviewRepository
            .Setup(x => x.GetBudgetReviewsAsync(userId, start, end))
            .ReturnsAsync(expected);

        var result = await CreateSut().GetCurrentUserBudgetReviewsAsync(start, end);

        Assert.Same(expected, result);
        _budgetReviewRepository.Verify(x => x.GetBudgetReviewsAsync(userId, start, end), Times.Once);
    }

    [Fact]
    public async Task GetBudgetReviewTemplateAsync_WhenNoDateProvided_UsesUtcNow()
    {
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var positions = new PagedResult<Position> { Items = [new Position { Name = "Pos1", Currency = Currency.PLN }] };
        var rates = new RatesModel { Date = today, BaseCurrency = Currency.EUR, Rates = [] };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _ratesModelService.Setup(x => x.GetRatesModelAsync(today)).ReturnsAsync(rates);
        _positionService.Setup(x => x.GetCurrentUserPositionsAsync(It.IsAny<PaginationRequest>())).ReturnsAsync(positions);
        _budgetReviewRepository.Setup(x => x.GetLatestBudgetReviewAsync(userId)).ReturnsAsync((BudgetReview?)null);

        var result = await CreateSut().GetBudgetReviewTemplateAsync(null);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(today, result.Date);
        Assert.NotNull(result.RatesModel);
        Assert.Single(result.Positions);
        Assert.Equal("Pos1", result.Positions[0].Name);
    }

    [Fact]
    public async Task DeleteBudgetReviewAsync_WhenCalled_DeletesAndReturnsId()
    {
        var reviewId = Guid.NewGuid();
        _budgetReviewRepository.Setup(x => x.DeleteBudgetReviewAsync(reviewId)).Returns(Task.CompletedTask);

        var result = await CreateSut().DeleteBudgetReviewAsync(reviewId);

        Assert.Equal(reviewId, result);
        _budgetReviewRepository.Verify(x => x.DeleteBudgetReviewAsync(reviewId), Times.Once);
    }

    [Fact]
    public async Task PublishBudgetReviewAsync_WhenNewReview_CreatesWithRatesAndPositions()
    {
        var userId = Guid.NewGuid();
        var publishDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var publishRequest = new PublishBudgetReviewRequest
        {
            Date = publishDate,
            ReviewedPositions = [new PositionToPublish { Name = "Item1", Currency = Currency.USD, Amount = 100 }]
        };
        var rates = new RatesModel { Date = publishDate, BaseCurrency = Currency.EUR, Rates = [] };
        var reviewedPositions = new List<ReviewedPosition> { ReviewedPosition.FromPosition(new BasePosition { Name = "Item1", Currency = Currency.USD }, 100) };
        var savedReview = new BudgetReview { Id = Guid.NewGuid(), UserId = userId, Date = publishDate };

        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _ratesModelService.Setup(x => x.GetRatesModelAsync(publishDate)).ReturnsAsync(rates);
        _budgetReviewRepository.Setup(x => x.GetBudgetReviewAsync(userId, publishDate)).ReturnsAsync((BudgetReview?)null);
        _mapper.Setup(x => x.Map<List<ReviewedPosition>>(publishRequest.ReviewedPositions)).Returns(reviewedPositions);
        _budgetReviewRepository.Setup(x => x.UpsertBudgetReviewAsync(It.IsAny<BudgetReview>())).ReturnsAsync(savedReview);

        var result = await CreateSut().PublishBudgetReviewAsync(publishRequest);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(publishDate, result.Date);
        _budgetReviewRepository.Verify(x => x.UpsertBudgetReviewAsync(It.IsAny<BudgetReview>()), Times.Once);
    }

    [Fact]
    public async Task PublishBudgetReviewAsync_WhenIdProvidedAndSameDate_UpdatesExistingReview()
    {
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var publishDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var publishRequest = new PublishBudgetReviewRequest
        {
            Id = reviewId,
            Date = publishDate,
            ReviewedPositions = [new PositionToPublish { Name = "Item1", Currency = Currency.USD, Amount = 50 }]
        };
        var existingReview = new BudgetReview { Id = reviewId, UserId = userId, Date = publishDate };
        var reviewedPositions = new List<ReviewedPosition> { ReviewedPosition.FromPosition(new BasePosition { Name = "Item1", Currency = Currency.USD }, 50) };

        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _budgetReviewRepository.Setup(x => x.GetBudgetReviewAsync(reviewId)).ReturnsAsync(existingReview);
        _mapper.Setup(x => x.Map<List<ReviewedPosition>>(publishRequest.ReviewedPositions)).Returns(reviewedPositions);
        _budgetReviewRepository.Setup(x => x.UpsertBudgetReviewAsync(existingReview)).ReturnsAsync(existingReview);

        var result = await CreateSut().PublishBudgetReviewAsync(publishRequest);

        Assert.Same(existingReview, result);
        _budgetReviewRepository.Verify(x => x.UpsertBudgetReviewAsync(existingReview), Times.Once);
        _ratesModelService.Verify(x => x.GetRatesModelAsync(It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task PublishBudgetReviewAsync_WhenReviewByDateExists_DeletesOldAndCreatesNew()
    {
        var userId = Guid.NewGuid();
        var publishDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var publishRequest = new PublishBudgetReviewRequest
        {
            Date = publishDate,
            ReviewedPositions = [new PositionToPublish { Name = "Item1", Currency = Currency.USD, Amount = 100 }]
        };
        var existingByDate = new BudgetReview { Id = Guid.NewGuid(), UserId = userId, Date = publishDate, RatesModel = new RatesModel() };
        var reviewedPositions = new List<ReviewedPosition> { ReviewedPosition.FromPosition(new BasePosition { Name = "Item1", Currency = Currency.USD }, 100) };
        var savedReview = new BudgetReview { Id = Guid.NewGuid(), UserId = userId, Date = publishDate };

        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _budgetReviewRepository.Setup(x => x.GetBudgetReviewAsync(userId, publishDate)).ReturnsAsync(existingByDate);
        _budgetReviewRepository.Setup(x => x.DeleteBudgetReviewAsync(existingByDate.Id)).Returns(Task.CompletedTask);
        _mapper.Setup(x => x.Map<List<ReviewedPosition>>(publishRequest.ReviewedPositions)).Returns(reviewedPositions);
        _budgetReviewRepository.Setup(x => x.UpsertBudgetReviewAsync(It.IsAny<BudgetReview>())).ReturnsAsync(savedReview);

        var result = await CreateSut().PublishBudgetReviewAsync(publishRequest);

        Assert.Equal(userId, result.UserId);
        _budgetReviewRepository.Verify(x => x.DeleteBudgetReviewAsync(existingByDate.Id), Times.Once);
        _budgetReviewRepository.Verify(x => x.UpsertBudgetReviewAsync(It.IsAny<BudgetReview>()), Times.Once);
    }
}
