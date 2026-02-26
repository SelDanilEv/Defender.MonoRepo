using Defender.Common.DB.Pagination;
using Defender.Common.Interfaces;
using Defender.Portal.Application.Common.Interfaces.Repositories;
using Defender.Portal.Application.Services.Accounts;
using Defender.Portal.Domain.Entities;
using Defender.Portal.Domain.Enums;

namespace Defender.Portal.Tests.Services;

public class UserActivityServiceTests
{
    private readonly Mock<IUserActivityRepository> _userActivityRepository = new();
    private readonly Mock<ICurrentAccountAccessor> _currentAccountAccessor = new();

    [Fact]
    public async Task CreateUserActivity_WhenCalled_UsesCurrentUserAndPersists()
    {
        var userId = Guid.NewGuid();
        var expectedCode = ActivityCode.CreateUserWithPassword;
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _userActivityRepository
            .Setup(x => x.CreateUserActivityAsync(It.IsAny<PortalUserActivity>()))
            .ReturnsAsync((PortalUserActivity activity) => activity);
        var sut = new UserActivityService(_userActivityRepository.Object, _currentAccountAccessor.Object);

        var result = await sut.CreateUserActivity(expectedCode, "created");

        Assert.Equal(userId, result.UserId);
        Assert.Equal(expectedCode, result.Code);
        Assert.Equal("created", result.Message);
    }

    [Fact]
    public async Task GetUserActivities_WhenNoUserFilter_ReturnsRepositoryResult()
    {
        var request = new PaginationRequest();
        var expected = new PagedResult<PortalUserActivity>
        {
            Items = [new PortalUserActivity()]
        };
        _userActivityRepository
            .Setup(x => x.GetUserActivitiesAsync(It.IsAny<PaginationSettings<PortalUserActivity>>()))
            .ReturnsAsync(expected);
        var sut = new UserActivityService(_userActivityRepository.Object, _currentAccountAccessor.Object);

        var result = await sut.GetUserActivities(request, null);

        Assert.Same(expected, result);
        _userActivityRepository.Verify(
            x => x.GetUserActivitiesAsync(It.IsAny<PaginationSettings<PortalUserActivity>>()),
            Times.Once);
    }
}
