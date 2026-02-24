using Defender.BudgetTracker.Application.Models.DiagramSetups;
using Defender.BudgetTracker.Application.Services;
using Defender.BudgetTracker.Domain.Entities.DiagramSetup;
using Defender.BudgetTracker.Domain.Enums;
using Moq;

namespace Defender.BudgetTracker.Tests.Services;

public class DiagramSetupServiceTests
{
    private readonly Mock<Defender.BudgetTracker.Application.Common.Interfaces.Repositories.IDiagramSetupRepository> _repository = new();
    private readonly Mock<Defender.Common.Interfaces.ICurrentAccountAccessor> _currentAccountAccessor = new();

    private DiagramSetupService CreateSut()
        => new(_repository.Object, _currentAccountAccessor.Object);

    [Fact]
    public async Task GetCurrentUserDiagramSetupAsync_WhenSetupExists_ReturnsIt()
    {
        var userId = Guid.NewGuid();
        var setup = new DiagramSetup
        {
            UserId = userId,
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
            LastMonths = 6,
            MainCurrency = DiagramSetupCurrency.ALL
        };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _repository.Setup(x => x.GetDiagramSetupByUserIdAsync(userId)).ReturnsAsync(setup);

        var result = await CreateSut().GetCurrentUserDiagramSetupAsync();

        Assert.Same(setup, result);
        _repository.Verify(x => x.GetDiagramSetupByUserIdAsync(userId), Times.Once);
        _repository.Verify(x => x.SetDiagramSetupAsync(It.IsAny<DiagramSetup>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentUserDiagramSetupAsync_WhenSetupMissing_CreatesDefault()
    {
        var userId = Guid.NewGuid();
        var created = new DiagramSetup
        {
            UserId = userId,
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
            LastMonths = 6,
            MainCurrency = DiagramSetupCurrency.ALL
        };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _repository.Setup(x => x.GetDiagramSetupByUserIdAsync(userId)).ReturnsAsync((DiagramSetup?)null);
        _repository.Setup(x => x.SetDiagramSetupAsync(It.IsAny<DiagramSetup>())).ReturnsAsync(created);

        var result = await CreateSut().GetCurrentUserDiagramSetupAsync();

        Assert.NotNull(result);
        _repository.Verify(x => x.SetDiagramSetupAsync(It.Is<DiagramSetup>(s => s.UserId == userId)), Times.Once);
    }

    [Fact]
    public async Task UpdateDiagramSetupAsync_WhenCalled_SetsSetupForCurrentUser()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateMainDiagramSetupRequest
        {
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1),
            LastMonths = 12,
            MainCurrency = DiagramSetupCurrency.PLN
        };
        var setup = request.MapToDiagramSetup(userId);
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _repository.Setup(x => x.SetDiagramSetupAsync(It.IsAny<DiagramSetup>())).ReturnsAsync(setup);

        var result = await CreateSut().UpdateDiagramSetupAsync(request);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(12, result.LastMonths);
        Assert.Equal(DiagramSetupCurrency.PLN, result.MainCurrency);
        _repository.Verify(x => x.SetDiagramSetupAsync(It.Is<DiagramSetup>(s => s.UserId == userId && s.LastMonths == 12)), Times.Once);
    }
}
