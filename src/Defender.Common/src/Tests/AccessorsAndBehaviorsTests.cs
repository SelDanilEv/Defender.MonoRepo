using System.Security.Claims;
using Defender.Common.Accessors;
using Defender.Common.Attributes;
using Defender.Common.Behaviors;
using Defender.Common.Enums;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Common.Interfaces;
using Defender.Common.Wrapper.Internal;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Defender.Common.Tests;

public class AccessorsAndBehaviorsTests
{
    [Fact]
    public async Task GetAuthenticationHeader_WhenWithoutAuthorization_ReturnsBearerWithoutToken()
    {
        var accessor = CreateAuthenticationHeaderAccessor("Bearer token");

        var result = await accessor.GetAuthenticationHeader(AuthorizationType.WithoutAuthorization);

        Assert.Equal("Bearer", result.Scheme);
        Assert.Null(result.Parameter);
    }

    [Fact]
    public async Task GetAuthenticationHeader_WhenUserAuthorizationAndTokenIsValid_ReturnsUserToken()
    {
        var accessor = CreateAuthenticationHeaderAccessor("Bearer abc123");

        var result = await accessor.GetAuthenticationHeader(AuthorizationType.User);

        Assert.Equal("Bearer", result.Scheme);
        Assert.Equal("abc123", result.Parameter);
    }

    [Fact]
    public async Task GetAuthenticationHeader_WhenUserAuthorizationAndTokenIsInvalid_ReturnsDefaultBearer()
    {
        var accessor = CreateAuthenticationHeaderAccessor("invalid-token");

        var result = await accessor.GetAuthenticationHeader(AuthorizationType.User);

        Assert.Equal("Bearer", result.Scheme);
        Assert.Null(result.Parameter);
    }

    [Fact]
    public async Task GetAuthenticationHeader_WhenServiceAuthorization_ReturnsInternalJwt()
    {
        Environment.SetEnvironmentVariable("Defender_App_JwtSecret", "0123456789ABCDEF0123456789ABCDEF", EnvironmentVariableTarget.Process);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("JwtTokenIssuer", "defender-tests")
            ])
            .Build();
        var currentAccountAccessor = new Mock<ICurrentAccountAccessor>();
        var accessor = new AuthenticationHeaderAccessor(configuration, currentAccountAccessor.Object);

        var result = await accessor.GetAuthenticationHeader(AuthorizationType.Service);

        Assert.Equal("Bearer", result.Scheme);
        Assert.False(string.IsNullOrWhiteSpace(result.Parameter));
    }

    [Fact]
    public async Task GetAuthenticationHeader_WhenAuthorizationTypeIsUnknown_ReturnsDefaultBearer()
    {
        var accessor = CreateAuthenticationHeaderAccessor("Bearer abc123");

        var result = await accessor.GetAuthenticationHeader((AuthorizationType)999);

        Assert.Equal("Bearer", result.Scheme);
        Assert.Null(result.Parameter);
    }

    [Fact]
    public void GetAccountId_WhenClaimIsPresent_ReturnsGuid()
    {
        var accountId = Guid.NewGuid();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = BuildPrincipal(
                [
                    new Claim(Defender.Common.Consts.ClaimTypes.NameIdentifier, accountId.ToString())
                ])
            }
        };
        var accessor = new CurrentAccountAccessor(httpContextAccessor);

        var result = accessor.GetAccountId();

        Assert.Equal(accountId, result);
    }

    [Fact]
    public void GetAccountId_WhenClaimMissing_ThrowsServiceException()
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = BuildPrincipal([]) }
        };
        var accessor = new CurrentAccountAccessor(httpContextAccessor);

        var exception = Assert.Throws<ServiceException>(() => accessor.GetAccountId());

        Assert.True(exception.IsErrorCode(ErrorCode.CM_InvalidUserJWT));
    }

    [Fact]
    public void GetRoles_WhenNoHttpContext_ReturnsEmptyList()
    {
        var accessor = new CurrentAccountAccessor(new HttpContextAccessor());

        var result = accessor.GetRoles();

        Assert.Empty(result);
    }

    [Fact]
    public void GetHighestRole_WhenClaimsContainAdmin_ReturnsAdmin()
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = BuildPrincipal(
                [
                    new Claim(ClaimTypes.Role, Role.Admin.ToString()),
                    new Claim(ClaimTypes.Role, Role.User.ToString())
                ])
            }
        };
        var accessor = new CurrentAccountAccessor(httpContextAccessor);

        var result = accessor.GetHighestRole();

        Assert.Equal(Role.Admin, result);
    }

    [Fact]
    public void Token_WhenAuthorizationHeaderExists_ReturnsToken()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer from-header";
        var accessor = new CurrentAccountAccessor(new HttpContextAccessor { HttpContext = httpContext });

        var token = accessor.Token;

        Assert.Equal("Bearer from-header", token);
    }

    [Fact]
    public void HasRole_WhenRolePresent_ReturnsTrue()
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = BuildPrincipal(
                [
                    new Claim(ClaimTypes.Role, Role.User.ToString())
                ])
            }
        };
        var accessor = new CurrentAccountAccessor(httpContextAccessor);

        var result = accessor.HasRole(Role.User);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidationBehavior_WhenValidationFails_ThrowsValidationException()
    {
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest(string.Empty);

        var exception = await Assert.ThrowsAsync<Defender.Common.Exceptions.ValidationException>(
            () => behavior.Handle(request, _ => Task.FromResult("ok"), CancellationToken.None));

        Assert.NotEmpty(exception.Errors);
    }

    [Fact]
    public async Task ValidationBehavior_WhenValidationPasses_ExecutesNext()
    {
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var result = await behavior.Handle(
            new TestRequest("valid"),
            _ => Task.FromResult("done"),
            CancellationToken.None);

        Assert.Equal("done", result);
    }

    [Fact]
    public async Task LoggingBehavior_WhenExecuted_ReturnsResponse()
    {
        var logger = new Mock<ILogger<LoggingBehavior<TestRequest, string>>>();
        var behavior = new LoggingBehavior<TestRequest, string>(logger.Object);

        var result = await behavior.Handle(
            new TestRequest("value"),
            _ => Task.FromResult("response"),
            CancellationToken.None);

        Assert.Equal("response", result);
    }

    [Fact]
    public async Task UnhandledExceptionBehavior_WhenHandlerThrows_Rethrows()
    {
        var logger = new Mock<ILogger<TestRequest>>();
        var behavior = new UnhandledExceptionBehavior<TestRequest, string>(logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(
                new TestRequest("value"),
                _ => throw new InvalidOperationException("boom"),
                CancellationToken.None));
    }

    [Fact]
    public void AuthAttribute_WhenRolesProvided_UsesCommaSeparatedRoles()
    {
        var attribute = new AuthAttribute("Admin", "User");

        Assert.Equal("Admin,User", attribute.Roles);
    }

    private static AuthenticationHeaderAccessor CreateAuthenticationHeaderAccessor(string token)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("JwtTokenIssuer", "defender-tests")
            ])
            .Build();
        var currentAccountAccessor = new Mock<ICurrentAccountAccessor>();
        currentAccountAccessor.SetupGet(x => x.Token).Returns(token);
        return new AuthenticationHeaderAccessor(configuration, currentAccountAccessor.Object);
    }

    private static ClaimsPrincipal BuildPrincipal(IEnumerable<Claim> claims)
        => new(new ClaimsIdentity(claims, "test"));

    public sealed record TestRequest(string Name) : IRequest<string>;

    public sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
