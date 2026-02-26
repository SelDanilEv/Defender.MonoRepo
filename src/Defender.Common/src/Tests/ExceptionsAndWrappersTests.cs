using System.Net.Http.Headers;
using Defender.Common.Clients.Base;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Common.Interfaces;
using Defender.Common.Wrapper;
using Defender.Common.Wrapper.Internal;

namespace Defender.Common.Tests;

public class ExceptionsAndWrappersTests
{
    [Fact]
    public void ServiceException_WhenBuiltFromErrorCode_HasExpectedMessage()
    {
        var exception = new ServiceException(ErrorCode.CM_NotFound);

        Assert.True(exception.IsErrorCode(ErrorCode.CM_NotFound));
    }

    [Fact]
    public void ValidationException_WhenFailuresProvided_GroupsErrorsByProperty()
    {
        var failures = new[]
        {
            new FluentValidation.Results.ValidationFailure("Name", "Required"),
            new FluentValidation.Results.ValidationFailure("Name", "Too short"),
            new FluentValidation.Results.ValidationFailure("Email", "Invalid")
        };

        var exception = new ValidationException("validation", failures);

        Assert.Equal(2, exception.Errors["Name"].Length);
        Assert.Single(exception.Errors["Email"]);
    }

    [Fact]
    public void ApiExceptionToServiceException_WhenResponseIsEmpty_ReturnsUnknownServiceException()
    {
        var exception = CreateApiException(string.Empty);

        var result = exception.ToServiceException();

        Assert.True(result.IsErrorCode(ErrorCode.Unknown));
    }

    [Fact]
    public void ApiExceptionToServiceException_WhenProblemDetailsContainsErrorCode_ReturnsMappedServiceException()
    {
        var exception = CreateApiException("{\"detail\":\"CM_NotFound\"}");

        var result = exception.ToServiceException();

        Assert.True(result.IsErrorCode(ErrorCode.CM_NotFound));
    }

    [Fact]
    public async Task BaseSwaggerWrapperExecuteSafelyAsync_WhenApiExceptionThrown_ConvertsToServiceException()
    {
        var wrapper = new TestSwaggerWrapper();

        var exception = await Assert.ThrowsAsync<ServiceException>(
            () => wrapper.ExecuteSafe<int>(() => throw CreateApiException("{\"detail\":\"CM_NotFound\"}")));

        Assert.True(exception.IsErrorCode(ErrorCode.CM_NotFound));
    }

    [Fact]
    public async Task BaseSwaggerWrapperExecuteSafelyAsync_WhenUnexpectedExceptionThrown_ReturnsUnhandledError()
    {
        var wrapper = new TestSwaggerWrapper();

        var exception = await Assert.ThrowsAsync<ServiceException>(
            () => wrapper.ExecuteSafe<int>(() => throw new InvalidOperationException("boom")));

        Assert.True(exception.IsErrorCode(ErrorCode.UnhandledError));
    }

    [Fact]
    public async Task BaseInternalSwaggerWrapper_WhenAuthorizationRequired_SetsHeaderAndExecutes()
    {
        var client = new Mock<IBaseServiceClient>();
        var authenticationAccessor = new Mock<IAuthenticationHeaderAccessor>();
        authenticationAccessor
            .Setup(x => x.GetAuthenticationHeader(AuthorizationType.Service))
            .ReturnsAsync(new AuthenticationHeaderValue("Bearer", "token"));
        var wrapper = new TestInternalSwaggerWrapper(client.Object, authenticationAccessor.Object);

        var result = await wrapper.ExecuteSafeWithAuth(() => Task.FromResult(7), AuthorizationType.Service);

        Assert.Equal(7, result);
        client.Verify(x => x.SetAuthorizationHeader(It.IsAny<AuthenticationHeaderValue>()), Times.Once);
    }

    [Fact]
    public async Task BaseInternalSwaggerWrapper_WhenWithoutAuthorization_DoesNotSetHeader()
    {
        var client = new Mock<IBaseServiceClient>();
        var authenticationAccessor = new Mock<IAuthenticationHeaderAccessor>();
        var wrapper = new TestInternalSwaggerWrapper(client.Object, authenticationAccessor.Object);

        var result = await wrapper.ExecuteUnsafeWithAuth(() => Task.FromResult("ok"), AuthorizationType.WithoutAuthorization);

        Assert.Equal("ok", result);
        client.Verify(x => x.SetAuthorizationHeader(It.IsAny<AuthenticationHeaderValue>()), Times.Never);
    }

    [Fact]
    public void ForbiddenAccessException_WhenConstructed_DefaultErrorCodeIsForbidden()
    {
        var exception = new ForbiddenAccessException();

        Assert.True(exception.IsErrorCode(ErrorCode.CM_ForbiddenAccess));
    }

    [Fact]
    public void NotFoundException_WhenConstructed_DefaultErrorCodeIsNotFound()
    {
        var exception = new NotFoundException();

        Assert.True(exception.IsErrorCode(ErrorCode.CM_NotFound));
    }

    [Fact]
    public void ApiExceptionOfT_WhenConstructed_ExposesResult()
    {
        var exception = new ApiException<int>(
            "msg",
            500,
            "{}",
            new Dictionary<string, IEnumerable<string>>(),
            42,
            new Exception("inner"));

        Assert.Equal(42, exception.Result);
    }

    [Fact]
    public async Task BaseSwaggerWrapperExecuteSafelyAsyncVoid_WhenApiExceptionThrown_ConvertsToServiceException()
    {
        var wrapper = new TestSwaggerWrapper();

        var exception = await Assert.ThrowsAsync<ServiceException>(
            () => wrapper.ExecuteSafeVoid(() => throw CreateApiException("{\"detail\":\"CM_NotFound\"}")));

        Assert.True(exception.IsErrorCode(ErrorCode.CM_NotFound));
    }

    [Fact]
    public async Task BaseSwaggerWrapperExecuteUnsafelyAsyncVoid_WhenExceptionThrown_Rethrows()
    {
        var wrapper = new TestSwaggerWrapper();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => wrapper.ExecuteUnsafeVoid(() => throw new InvalidOperationException("boom")));
    }

    [Fact]
    public async Task BaseInternalSwaggerWrapperExecuteSafelyAsyncVoid_WhenAuthorizationRequired_SetsHeader()
    {
        var client = new Mock<IBaseServiceClient>();
        var authenticationAccessor = new Mock<IAuthenticationHeaderAccessor>();
        authenticationAccessor
            .Setup(x => x.GetAuthenticationHeader(AuthorizationType.User))
            .ReturnsAsync(new AuthenticationHeaderValue("Bearer", "token"));
        var wrapper = new TestInternalSwaggerWrapper(client.Object, authenticationAccessor.Object);

        await wrapper.ExecuteSafeVoidWithAuth(() => Task.CompletedTask, AuthorizationType.User);

        client.Verify(x => x.SetAuthorizationHeader(It.IsAny<AuthenticationHeaderValue>()), Times.Once);
    }

    private static ApiException CreateApiException(string response)
    {
        return new ApiException(
            "request failed",
            400,
            response,
            new Dictionary<string, IEnumerable<string>>(),
            new InvalidOperationException("inner"));
    }

    private sealed class TestSwaggerWrapper : BaseSwaggerWrapper
    {
        public Task<T> ExecuteSafe<T>(Func<Task<T>> action) => ExecuteSafelyAsync(action);
        public Task ExecuteSafeVoid(Func<Task> action) => ExecuteSafelyAsync(action);
        public Task ExecuteUnsafeVoid(Func<Task> action) => ExecuteUnsafelyAsync(action);
    }

    private sealed class TestInternalSwaggerWrapper(
        IBaseServiceClient client,
        IAuthenticationHeaderAccessor authenticationHeaderAccessor)
        : BaseInternalSwaggerWrapper(client, authenticationHeaderAccessor)
    {
        public Task<T> ExecuteSafeWithAuth<T>(Func<Task<T>> action, AuthorizationType authorizationType)
            => ExecuteSafelyAsync(action, authorizationType);

        public Task<T> ExecuteUnsafeWithAuth<T>(Func<Task<T>> action, AuthorizationType authorizationType)
            => ExecuteUnsafelyAsync(action, authorizationType);

        public Task ExecuteSafeVoidWithAuth(Func<Task> action, AuthorizationType authorizationType)
            => ExecuteSafelyAsync(action, authorizationType);
    }
}
