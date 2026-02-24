using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Application.Models.Positions;
using Defender.BudgetTracker.Application.Services;
using Defender.BudgetTracker.Domain.Entities.Position;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.DB.Pagination;
using Moq;

namespace Defender.BudgetTracker.Tests.Services;

public class PositionServiceTests
{
    private readonly Mock<IPositionRepository> _positionRepository = new();
    private readonly Mock<Defender.Common.Interfaces.ICurrentAccountAccessor> _currentAccountAccessor = new();

    private PositionService CreateSut()
        => new(_positionRepository.Object, _currentAccountAccessor.Object);

    [Fact]
    public async Task GetCurrentUserPositionsAsync_WhenCalled_UsesCurrentUserId()
    {
        var userId = Guid.NewGuid();
        var paginationRequest = new PaginationRequest();
        var expected = new PagedResult<Position> { Items = [new Position()] };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _positionRepository
            .Setup(x => x.GetPositionsAsync(paginationRequest, userId))
            .ReturnsAsync(expected);

        var result = await CreateSut().GetCurrentUserPositionsAsync(paginationRequest);

        Assert.Same(expected, result);
        _positionRepository.Verify(x => x.GetPositionsAsync(paginationRequest, userId), Times.Once);
    }

    [Fact]
    public async Task CreatePositionAsync_WhenCalled_AssignsCurrentUserId()
    {
        var userId = Guid.NewGuid();
        var request = new CreatePositionRequest
        {
            Name = "Salary",
            Currency = Currency.PLN,
            Tags = ["income"],
            OrderPriority = 1
        };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _positionRepository
            .Setup(x => x.CreatePositionAsync(It.IsAny<Position>()))
            .ReturnsAsync((Position p) => p);

        var result = await CreateSut().CreatePositionAsync(request);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Currency, result.Currency);
        _positionRepository.Verify(x => x.CreatePositionAsync(It.IsAny<Position>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePositionAsync_WhenCalled_DelegatesToRepository()
    {
        var positionId = Guid.NewGuid();
        var request = new UpdatePositionRequest
        {
            Id = positionId,
            Name = "Updated",
            Currency = Currency.USD
        };
        var updated = new Position { Id = positionId, Name = "Updated" };
        _positionRepository
            .Setup(x => x.UpdatePositionAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Position>>()))
            .ReturnsAsync(updated);

        var result = await CreateSut().UpdatePositionAsync(request);

        Assert.Equal("Updated", result.Name);
        _positionRepository.Verify(x => x.UpdatePositionAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Position>>()), Times.Once);
    }

    [Fact]
    public async Task DeletePositionAsync_WhenCalled_DeletesAndReturnsId()
    {
        var positionId = Guid.NewGuid();
        _positionRepository.Setup(x => x.DeletePositionAsync(positionId)).Returns(Task.CompletedTask);

        var result = await CreateSut().DeletePositionAsync(positionId);

        Assert.Equal(positionId, result);
        _positionRepository.Verify(x => x.DeletePositionAsync(positionId), Times.Once);
    }
}
