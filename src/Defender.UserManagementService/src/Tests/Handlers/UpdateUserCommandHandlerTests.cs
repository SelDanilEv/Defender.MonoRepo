using Defender.Common.Consts;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Common.Interfaces;
using Defender.UserManagementService.Application.Common.Interfaces.Services;
using Defender.UserManagementService.Application.Models;
using Defender.UserManagementService.Application.Modules.Users.Commands;
using Defender.UserManagementService.Domain.Entities;

namespace Defender.UserManagementService.Tests.Handlers;

public class UpdateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenAdmin_UpdatesWithoutAccessCodeCheck()
    {
        var accessor = new Mock<ICurrentAccountAccessor>();
        accessor.Setup(a => a.GetRoles()).Returns(new List<string> { Roles.Admin });
        var userService = new Mock<IUserManagementService>();
        var accessCodeService = new Mock<IAccessCodeService>();
        var authService = new Mock<Defender.Common.Interfaces.IAuthorizationCheckingService>();
        var userId = Guid.NewGuid();
        var updated = new UserInfo { Id = userId };
        authService.Setup(s => s.ExecuteWithAuthCheckAsync(userId, It.IsAny<Func<Task<UserInfo>>>(), false, ErrorCode.CM_ForbiddenAccess))
            .Returns<Guid, Func<Task<UserInfo>>, bool, ErrorCode>((_, f, _, _) => f!.Invoke());
        userService.Setup(s => s.UpdateUserAsync(It.IsAny<UpdateUserInfoRequest>())).ReturnsAsync(updated);

        var handler = new UpdateUserCommandHandler(
            accessor.Object,
            userService.Object,
            accessCodeService.Object,
            authService.Object);
        var command = new UpdateUserCommand { Id = userId, Nickname = "new" };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Same(updated, result);
        accessCodeService.Verify(s => s.VerifyUpdateUserAccessCodeAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNonAdminWithValidCode_UpdatesUser()
    {
        var accessor = new Mock<ICurrentAccountAccessor>();
        accessor.Setup(a => a.GetRoles()).Returns(new List<string> { Roles.User });
        var userService = new Mock<IUserManagementService>();
        var accessCodeService = new Mock<IAccessCodeService>();
        accessCodeService.Setup(s => s.VerifyUpdateUserAccessCodeAsync(999)).ReturnsAsync(true);
        var authService = new Mock<Defender.Common.Interfaces.IAuthorizationCheckingService>();
        var userId = Guid.NewGuid();
        var updated = new UserInfo { Id = userId };
        authService.Setup(s => s.ExecuteWithAuthCheckAsync(userId, It.IsAny<Func<Task<UserInfo>>>(), false, ErrorCode.CM_ForbiddenAccess))
            .Returns<Guid, Func<Task<UserInfo>>, bool, ErrorCode>((_, f, _, _) => f!.Invoke());
        userService.Setup(s => s.UpdateUserAsync(It.IsAny<UpdateUserInfoRequest>())).ReturnsAsync(updated);

        var handler = new UpdateUserCommandHandler(
            accessor.Object,
            userService.Object,
            accessCodeService.Object,
            authService.Object);
        var command = new UpdateUserCommand { Id = userId, Code = 999, Email = "e@b.com" };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Same(updated, result);
        accessCodeService.Verify(s => s.VerifyUpdateUserAccessCodeAsync(999), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNonAdminWithInvalidCode_ThrowsServiceException()
    {
        var accessor = new Mock<ICurrentAccountAccessor>();
        accessor.Setup(a => a.GetRoles()).Returns(new List<string> { Roles.User });
        var accessCodeService = new Mock<IAccessCodeService>();
        accessCodeService.Setup(s => s.VerifyUpdateUserAccessCodeAsync(111)).ReturnsAsync(false);
        var authService = new Mock<Defender.Common.Interfaces.IAuthorizationCheckingService>();
        var handler = new UpdateUserCommandHandler(
            accessor.Object,
            Mock.Of<IUserManagementService>(),
            accessCodeService.Object,
            authService.Object);
        var command = new UpdateUserCommand { Id = Guid.NewGuid(), Code = 111 };

        var ex = await Assert.ThrowsAsync<ServiceException>(() => handler.Handle(command, CancellationToken.None));

        Assert.True(ex.IsErrorCode(ErrorCode.BR_ACC_InvalidAccessCode));
    }

    [Fact]
    public async Task Handle_WhenNonAdminWithoutCode_CallsAsUserAndUpdates()
    {
        var accessor = new Mock<ICurrentAccountAccessor>();
        accessor.Setup(a => a.GetRoles()).Returns(new List<string> { Roles.Guest });
        var userService = new Mock<IUserManagementService>();
        var authService = new Mock<Defender.Common.Interfaces.IAuthorizationCheckingService>();
        var userId = Guid.NewGuid();
        var updated = new UserInfo { Id = userId };
        authService.Setup(s => s.ExecuteWithAuthCheckAsync(userId, It.IsAny<Func<Task<UserInfo>>>(), false, ErrorCode.CM_ForbiddenAccess))
            .Returns<Guid, Func<Task<UserInfo>>, bool, ErrorCode>((_, f, _, _) => f!.Invoke());
        UpdateUserCommand? captured = null;
        userService.Setup(s => s.UpdateUserAsync(It.IsAny<UpdateUserInfoRequest>()))
            .Callback<UpdateUserInfoRequest>(c => captured = (UpdateUserCommand)c)
            .ReturnsAsync(updated);

        var handler = new UpdateUserCommandHandler(
            accessor.Object,
            userService.Object,
            Mock.Of<IAccessCodeService>(),
            authService.Object);
        var command = new UpdateUserCommand { Id = userId, Email = "e@b.com", Nickname = "n" };

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Null(captured.Email);
        Assert.Null(captured.PhoneNumber);
    }
}
