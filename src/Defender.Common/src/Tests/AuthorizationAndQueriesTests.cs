using Defender.Common.DTOs;
using Defender.Common.Entities.AccountInfo;
using Defender.Common.Enums;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Common.Helpers;
using Defender.Common.Interfaces;
using Defender.Common.Modules.Home.Queries;
using Defender.Common.Service;

namespace Defender.Common.Tests;

public class AuthorizationAndQueriesTests
{
    [Fact]
    public async Task ExecuteWithAuthCheckAsync_WhenTargetMatchesCurrentUser_ExecutesAction()
    {
        var accountId = Guid.NewGuid();
        var currentAccessor = new Mock<ICurrentAccountAccessor>();
        currentAccessor.Setup(x => x.GetAccountId()).Returns(accountId);
        currentAccessor.Setup(x => x.GetHighestRole()).Returns(Role.User);
        var accountAccessor = new Mock<IAccountAccessor>();
        accountAccessor.Setup(x => x.GetAccountInfoById(accountId)).ReturnsAsync(
            new BaseAccountInfo { Id = accountId, Roles = new List<string> { Role.User.ToString() } });
        var service = new AuthorizationCheckingService(currentAccessor.Object, accountAccessor.Object);

        var result = await service.ExecuteWithAuthCheckAsync(accountId, () => Task.FromResult("ok"));

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task ExecuteWithAuthCheckAsync_WhenTargetMissing_ThrowsNotFound()
    {
        var currentAccessor = new Mock<ICurrentAccountAccessor>();
        currentAccessor.Setup(x => x.GetAccountId()).Returns(Guid.NewGuid());
        currentAccessor.Setup(x => x.GetHighestRole()).Returns(Role.User);
        var accountAccessor = new Mock<IAccountAccessor>();
        accountAccessor.Setup(x => x.GetAccountInfoById(It.IsAny<Guid>())).ReturnsAsync((BaseAccountInfo)null!);
        var service = new AuthorizationCheckingService(currentAccessor.Object, accountAccessor.Object);

        var exception = await Assert.ThrowsAsync<ServiceException>(
            () => service.ExecuteWithAuthCheckAsync(Guid.NewGuid(), () => Task.FromResult("value")));

        Assert.True(exception.IsErrorCode(ErrorCode.CM_NotFound));
    }

    [Fact]
    public async Task ExecuteWithAuthCheckAsync_WhenAdminTargetsAdminAndRequiresSuperAdmin_ThrowsForbidden()
    {
        var currentAccessor = new Mock<ICurrentAccountAccessor>();
        currentAccessor.Setup(x => x.GetAccountId()).Returns(Guid.NewGuid());
        currentAccessor.Setup(x => x.GetHighestRole()).Returns(Role.Admin);
        var accountAccessor = new Mock<IAccountAccessor>();
        accountAccessor.Setup(x => x.GetAccountInfoById(It.IsAny<Guid>())).ReturnsAsync(
            new BaseAccountInfo { Id = Guid.NewGuid(), Roles = new List<string> { Role.Admin.ToString() } });
        var service = new AuthorizationCheckingService(currentAccessor.Object, accountAccessor.Object);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.ExecuteWithAuthCheckAsync(Guid.NewGuid(), () => Task.FromResult("value"), true));
    }

    [Fact]
    public async Task ExecuteBasedOnUserRoleAsync_WhenActionExistsForRole_ExecutesThatAction()
    {
        var currentAccessor = new Mock<ICurrentAccountAccessor>();
        currentAccessor.Setup(x => x.GetHighestRole()).Returns(Role.User);
        var accountAccessor = new Mock<IAccountAccessor>();
        var service = new AuthorizationCheckingService(currentAccessor.Object, accountAccessor.Object);

        var result = await service.ExecuteBasedOnUserRoleAsync(
            superAdminAction: () => Task.FromResult("super"),
            adminAction: () => Task.FromResult("admin"),
            userAction: () => Task.FromResult("user"));

        Assert.Equal("user", result);
    }

    [Fact]
    public async Task ExecuteBasedOnUserRoleAsync_WhenNoActionForRole_ThrowsForbidden()
    {
        var currentAccessor = new Mock<ICurrentAccountAccessor>();
        currentAccessor.Setup(x => x.GetHighestRole()).Returns(Role.Admin);
        var accountAccessor = new Mock<IAccountAccessor>();
        var service = new AuthorizationCheckingService(currentAccessor.Object, accountAccessor.Object);

        var exception = await Assert.ThrowsAsync<ServiceException>(
            () => service.ExecuteBasedOnUserRoleAsync<string>(
                superAdminAction: () => Task.FromResult("super"),
                adminAction: null,
                userAction: () => Task.FromResult("user")));

        Assert.True(exception.IsErrorCode(ErrorCode.CM_ForbiddenAccess));
    }

    [Fact]
    public async Task AuthCheckQueryHandler_WhenCalled_ReturnsUserIdAndHighestRole()
    {
        var userId = Guid.NewGuid();
        var currentAccessor = new Mock<ICurrentAccountAccessor>();
        currentAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        currentAccessor.Setup(x => x.GetRoles()).Returns([Role.User.ToString()]);
        var handler = new AuthCheckQueryHandler(currentAccessor.Object);

        var result = await handler.Handle(new AuthCheckQuery(), CancellationToken.None);

        Assert.Equal(new AuthCheckDto(userId, Role.User), result);
    }

    [Fact]
    public async Task HealthCheckQueryHandler_WhenCalled_ReturnsHealthStatus()
    {
        var handler = new HealthCheckQueryHandler();

        var result = await handler.Handle(new HealthCheckQuery(), CancellationToken.None);

        Assert.Equal(new HealthCheckDto("Health"), result);
    }

    [Fact]
    public async Task GetConfigurationQueryHandler_WhenAdminLevel_ReturnsAllSecrets()
    {
        var accessor = new Mock<IMongoSecretAccessor>();
        accessor.Setup(x => x.GetSecretValueByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string key) => key switch
            {
                nameof(Secret.JwtSecret) => "0123456789ABCDEF0123456789ABCDEF",
                nameof(Secret.SecretsEncryptionKey) => "00112233445566778899AABBCCDDEEFF",
                _ => $"value-{key}"
            });
        SecretsHelper.Initialize(accessor.Object);
        var handler = new GetConfigurationQueryHandler();

        var result = await handler.Handle(new GetConfigurationQuery { Level = ConfigurationLevel.Admin }, CancellationToken.None);

        foreach (var secret in Enum.GetNames<Secret>())
        {
            Assert.True(result.ContainsKey(secret));
        }
    }
}
