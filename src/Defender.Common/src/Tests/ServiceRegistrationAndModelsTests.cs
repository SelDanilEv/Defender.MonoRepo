using Defender.Common.Configuration.Options;
using Defender.Common.DB.Model;
using Defender.Common.DTOs;
using Defender.Common.Entities;
using Defender.Common.Enums;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Common.Extension;
using Defender.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Defender.Common.Tests;

public class ServiceRegistrationAndModelsTests
{
    [Fact]
    public void AddCommonServices_WhenCalled_RegistersCoreCommonServices()
    {
        Environment.SetEnvironmentVariable(
            "Defender_App_MongoDBConnectionString",
            "mongodb://localhost:27017",
            EnvironmentVariableTarget.Process);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("MongoDbOptions:AppName", "UnitTest"),
                new KeyValuePair<string, string?>("MongoDbOptions:Environment", "local")
            ])
            .Build();

        services.AddCommonServices(configuration);

        Assert.Contains(services, x => x.ServiceType == typeof(ICurrentAccountAccessor));
        Assert.Contains(services, x => x.ServiceType == typeof(IAuthenticationHeaderAccessor));
        Assert.Contains(services, x => x.ServiceType == typeof(IMongoSecretAccessor));
        Assert.Contains(services, x => x.ServiceType == typeof(IAccountAccessor));
        Assert.Contains(services, x => x.ServiceType == typeof(IAuthorizationCheckingService));
    }

    [Fact]
    public void AccountDto_WhenPropertiesAssigned_RetainsValues()
    {
        var account = new AccountDto
        {
            Id = Guid.NewGuid(),
            Roles = new List<string> { Role.Admin.ToString() },
            IsBlocked = true,
            IsEmailVerified = true,
            IsPhoneVerified = false
        };

        Assert.True(account.IsBlocked);
        Assert.True(account.IsEmailVerified);
        Assert.False(account.IsPhoneVerified);
        Assert.True(account.IsAdmin);
    }

    [Fact]
    public void UserDto_WhenPropertiesAssigned_RetainsValues()
    {
        var createdDate = DateTime.UtcNow;
        var dto = new UserDto
        {
            Id = Guid.NewGuid(),
            Email = "user@site.com",
            PhoneNumber = "123",
            Nickname = "Nick",
            CreatedDate = createdDate
        };

        Assert.Equal("user@site.com", dto.Email);
        Assert.Equal("123", dto.PhoneNumber);
        Assert.Equal("Nick", dto.Nickname);
        Assert.Equal(createdDate, dto.CreatedDate);
    }

    [Fact]
    public void ApiExceptionToString_WhenCalled_ContainsResponse()
    {
        var exception = new ApiException(
            "message",
            400,
            "{\"detail\":\"CM_NotFound\"}",
            new Dictionary<string, IEnumerable<string>>(),
            new Exception("inner"));

        var text = exception.ToString();

        Assert.Contains("HTTP Response", text);
        Assert.Contains("CM_NotFound", text);
    }

    [Fact]
    public void ForbiddenAccessException_WhenCustomCodeProvided_UsesCustomMessage()
    {
        var exception = new ForbiddenAccessException(ErrorCode.CM_InvalidUserJWT);

        Assert.True(exception.IsErrorCode(ErrorCode.CM_InvalidUserJWT));
    }

    [Fact]
    public void NotFoundException_WhenCustomCodeProvided_UsesCustomMessage()
    {
        var exception = new NotFoundException(ErrorCode.CM_DatabaseIssue);

        Assert.True(exception.IsErrorCode(ErrorCode.CM_DatabaseIssue));
    }

    [Fact]
    public void FindModelRequest_WhenCleared_ReturnsEmptyFilter()
    {
        var request = FindModelRequest<TestEntity>
            .Init(x => x.Name, "A")
            .Clear();

        Assert.Equal(FilterDefinition<TestEntity>.Empty, request.BuildFilterDefinition());
    }

    [Fact]
    public void UpdateModelRequest_WhenInitializedFromModel_SetsModelId()
    {
        var model = new TestEntity { Id = Guid.NewGuid(), Name = "A" };
        var request = UpdateModelRequest<TestEntity>.Init(model);

        Assert.Equal(model.Id, request.ModelId);
    }

    [Fact]
    public void UpdateModelRequest_WhenConditionIsFalse_DoesNotAddUpdate()
    {
        var request = UpdateModelRequest<TestEntity>
            .Init(Guid.NewGuid())
            .Set(x => x.Name, "X", () => false)
            .Clear();

        Assert.NotNull(request.BuildUpdateDefinition());
    }

    private sealed class TestEntity : IBaseModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
