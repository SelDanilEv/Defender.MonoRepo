using Defender.Common.DB.Pagination;
using Defender.UserManagementService.Application.Common.Interfaces.Services;
using Defender.UserManagementService.Application.Modules.Users.Queries;
using Defender.UserManagementService.Domain.Entities;

namespace Defender.UserManagementService.Tests.Handlers;

public class GetUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCalled_ReturnsPagedResultFromService()
    {
        var service = new Mock<IUserManagementService>();
        var expected = new PagedResult<UserInfo> { TotalItemsCount = 0, Items = [] };
        var query = new GetUsersQuery { Page = 1, PageSize = 10 };
        service.Setup(s => s.GetUsersAsync(query)).ReturnsAsync(expected);
        var handler = new GetUsersQueryHandler(service.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Same(expected, result);
        service.Verify(s => s.GetUsersAsync(query), Times.Once);
    }
}
