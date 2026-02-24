using Defender.UserManagementService.Application.Common.Interfaces.Services;
using Defender.UserManagementService.Application.Modules.Users.Queries;
using Defender.UserManagementService.Domain.Entities;

namespace Defender.UserManagementService.Tests.Handlers;

public class GetUserByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCalled_ReturnsUserFromService()
    {
        var userId = Guid.NewGuid();
        var service = new Mock<IUserManagementService>();
        var expected = new UserInfo { Id = userId };
        service.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(expected);
        var handler = new GetUserByIdQueryHandler(service.Object);
        var query = new GetUserByIdQuery { UserId = userId };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Same(expected, result);
        service.Verify(s => s.GetUserByIdAsync(userId), Times.Once);
    }
}
