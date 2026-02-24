using Defender.UserManagementService.Application.Common.Interfaces.Services;
using Defender.UserManagementService.Application.Modules.Users.Queries;
using Defender.UserManagementService.Domain.Entities;

namespace Defender.UserManagementService.Tests.Handlers;

public class GetUserByLoginQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCalled_ReturnsUserFromService()
    {
        var service = new Mock<IUserManagementService>();
        var expected = new UserInfo { Email = "u@b.com" };
        service.Setup(s => s.GetUserByLoginAsync("u@b.com")).ReturnsAsync(expected);
        var handler = new GetUserByLoginQueryHandler(service.Object);
        var query = new GetUserByLoginQuery { Login = "u@b.com" };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Same(expected, result);
        service.Verify(s => s.GetUserByLoginAsync("u@b.com"), Times.Once);
    }
}
