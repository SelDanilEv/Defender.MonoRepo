using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Application.Models.RegularExpenseDiagramSetups;
using Defender.BudgetTracker.Application.Services;
using Defender.BudgetTracker.Domain.Entities.DiagramSetup;
using Defender.BudgetTracker.Domain.Enums;
using Defender.Common.Interfaces;
using Moq;

namespace Defender.BudgetTracker.Tests.Services;

public class RegularExpenseDiagramSetupServiceTests
{
    private readonly Mock<IRegularExpenseDiagramSetupRepository> _repository = new();
    private readonly Mock<ICurrentAccountAccessor> _currentAccountAccessor = new();

    [Fact]
    public async Task UpdateAsync_WhenCalled_AssignsCurrentUserAndNormalizesEndMonth()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateRegularExpenseDiagramSetupRequest
        {
            MainCurrency = Currency.PLN,
            LastMonths = 12,
            EndMonth = new DateOnly(2026, 8, 27)
        };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _repository.Setup(x => x.SetRegularExpenseDiagramSetupAsync(It.IsAny<RegularExpenseDiagramSetup>()))
            .ReturnsAsync((RegularExpenseDiagramSetup setup) => setup);
        var sut = new RegularExpenseDiagramSetupService(_repository.Object, _currentAccountAccessor.Object);

        var result = await sut.UpdateRegularExpenseDiagramSetupAsync(request);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(new DateOnly(2026, 8, 1), result.EndMonth);
        _repository.Verify(x => x.SetRegularExpenseDiagramSetupAsync(It.Is<RegularExpenseDiagramSetup>(setup =>
            setup.UserId == userId && setup.MainCurrency == Currency.PLN)), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenSetupExists_UsesCurrentUserId()
    {
        var userId = Guid.NewGuid();
        var expected = new RegularExpenseDiagramSetup
        {
            UserId = userId,
            MainCurrency = Currency.USD,
            LastMonths = 6,
            EndMonth = new DateOnly(2026, 8, 1)
        };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _repository.Setup(x => x.GetRegularExpenseDiagramSetupByUserIdAsync(userId)).ReturnsAsync(expected);
        var sut = new RegularExpenseDiagramSetupService(_repository.Object, _currentAccountAccessor.Object);

        var result = await sut.GetCurrentUserRegularExpenseDiagramSetupAsync();

        Assert.Same(expected, result);
        _repository.Verify(x => x.GetRegularExpenseDiagramSetupByUserIdAsync(userId), Times.Once);
    }
}
