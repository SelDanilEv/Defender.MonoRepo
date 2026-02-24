using Defender.UserManagementService.Application.Common.Interfaces.Services;
using Defender.UserManagementService.Application.Modules.Users.Queries;

namespace Defender.UserManagementService.Tests.Handlers;

public class CheckIsEmailTakenQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmailTaken_ReturnsTrue()
    {
        var service = new Mock<IUserManagementService>();
        service.Setup(s => s.CheckIfEmailTakenAsync("taken@b.com")).ReturnsAsync(true);
        var handler = new IsEmailTakenQueryHandler(service.Object);
        var query = new CheckIsEmailTakenQuery { Email = "taken@b.com" };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Handle_WhenEmailNotTaken_ReturnsFalse()
    {
        var service = new Mock<IUserManagementService>();
        service.Setup(s => s.CheckIfEmailTakenAsync("free@b.com")).ReturnsAsync(false);
        var handler = new IsEmailTakenQueryHandler(service.Object);
        var query = new CheckIsEmailTakenQuery { Email = "free@b.com" };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.False(result);
    }
}
