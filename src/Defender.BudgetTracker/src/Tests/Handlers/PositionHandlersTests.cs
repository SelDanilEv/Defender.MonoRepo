using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Modules.Positions.Commands;
using Defender.BudgetTracker.Application.Modules.Positions.Queries;
using Defender.BudgetTracker.Domain.Entities.Position;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.DB.Pagination;
using Moq;

namespace Defender.BudgetTracker.Tests.Handlers;

public class PositionHandlersTests
{
    private readonly Mock<IPositionService> _positionService = new();

    [Fact]
    public async Task GetPositionsQueryHandler_WhenCalled_DelegatesToService()
    {
        var request = new GetPositionsQuery();
        var expected = new PagedResult<Position> { Items = [new Position()] };
        _positionService.Setup(x => x.GetCurrentUserPositionsAsync(request)).ReturnsAsync(expected);

        var handler = new GetPositionsQueryHandler(_positionService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _positionService.Verify(x => x.GetCurrentUserPositionsAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreatePositionCommandHandler_WhenCalled_DelegatesToService()
    {
        var request = new CreatePositionCommand { Name = "P1", Currency = Currency.USD };
        var expected = new Position { Name = "P1" };
        _positionService.Setup(x => x.CreatePositionAsync(request)).ReturnsAsync(expected);

        var handler = new CreatePositionCommandHandler(_positionService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _positionService.Verify(x => x.CreatePositionAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdatePositionCommandHandler_WhenCalled_DelegatesToService()
    {
        var positionId = Guid.NewGuid();
        var request = new UpdatePositionCommand { Id = positionId, Name = "Updated" };
        var expected = new Position { Id = positionId, Name = "Updated" };
        _positionService.Setup(x => x.UpdatePositionAsync(request)).ReturnsAsync(expected);

        var handler = new UpdatePositionCommandHandler(_positionService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _positionService.Verify(x => x.UpdatePositionAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeletePositionCommandHandler_WhenCalled_DelegatesToService()
    {
        var positionId = Guid.NewGuid();
        var request = new DeletePositionCommand { Id = positionId };
        _positionService.Setup(x => x.DeletePositionAsync(positionId)).ReturnsAsync(positionId);

        var handler = new DeletePositionCommandHandler(_positionService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(positionId, result);
        _positionService.Verify(x => x.DeletePositionAsync(positionId), Times.Once);
    }
}
