using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Modules.DiagramSetups.Commands;
using Defender.BudgetTracker.Application.Modules.DiagramSetups.Queries;
using Defender.BudgetTracker.Domain.Entities.DiagramSetup;
using Moq;

namespace Defender.BudgetTracker.Tests.Handlers;

public class DiagramSetupHandlersTests
{
    private readonly Mock<IDiagramSetupService> _diagramSetupService = new();

    [Fact]
    public async Task GetMainDiagramSetupQueryHandler_WhenCalled_DelegatesToService()
    {
        var request = new GetMainDiagramSetupQuery();
        var expected = new DiagramSetup { LastMonths = 12 };
        _diagramSetupService.Setup(x => x.GetCurrentUserDiagramSetupAsync()).ReturnsAsync(expected);

        var handler = new GetMainDiagramSetupQueryHandler(_diagramSetupService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _diagramSetupService.Verify(x => x.GetCurrentUserDiagramSetupAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateMainDiagramSetupCommandHandler_WhenCalled_DelegatesToService()
    {
        var request = new UpdateMainDiagramSetupCommand { LastMonths = 6 };
        var expected = new DiagramSetup { LastMonths = 6 };
        _diagramSetupService.Setup(x => x.UpdateDiagramSetupAsync(request)).ReturnsAsync(expected);

        var handler = new UpdateMainDiagramSetupCommandHandler(_diagramSetupService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _diagramSetupService.Verify(x => x.UpdateDiagramSetupAsync(request), Times.Once);
    }
}
