using Defender.UserManagementService.Application.Common.Interfaces.Services;
using Defender.UserManagementService.Application.Modules.Users.Commands;
using Defender.UserManagementService.Domain.Entities;

namespace Defender.UserManagementService.Tests.Handlers;

public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCalled_DelegatesToUserManagementServiceAndReturnsUser()
    {
        var service = new Mock<IUserManagementService>();
        var expected = new UserInfo { Id = Guid.NewGuid(), Email = "a@b.com", Nickname = "nick" };
        service.Setup(s => s.CreateUserAsync("a@b.com", "+1", "nick")).ReturnsAsync(expected);
        var handler = new CreateUserCommandHandler(service.Object);
        var command = new CreateUserCommand { Email = "a@b.com", PhoneNumber = "+1", Nickname = "nick" };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Same(expected, result);
        service.Verify(s => s.CreateUserAsync("a@b.com", "+1", "nick"), Times.Once);
    }
}
