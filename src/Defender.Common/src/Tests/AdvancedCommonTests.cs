using AutoMapper;
using Defender.Common.DB.Pagination;
using Defender.Common.Entities;
using Defender.Common.Errors;
using Defender.Common.Extension;
using Defender.Common.Helpers;
using Defender.Common.Mapping;
using Defender.Common.Modules.Home.Queries;
using FluentValidation;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

namespace Defender.Common.Tests;

public class AdvancedCommonTests
{
    [Fact]
    public async Task ExecuteUnderTransactionAsync_WhenActionSucceeds_CommitsAndReturnsTrue()
    {
        var session = new Mock<IClientSessionHandle>();

        var result = await MongoTransactionHelper.ExecuteUnderTransactionAsync(
            session.Object,
            () => Task.CompletedTask);

        Assert.True(result);
        session.Verify(x => x.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
        session.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        session.Verify(x => x.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        session.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExecuteUnderTransactionAsync_WhenActionFails_AbortsAndReturnsFalse()
    {
        var session = new Mock<IClientSessionHandle>();

        var result = await MongoTransactionHelper.ExecuteUnderTransactionAsync(
            session.Object,
            () => throw new InvalidOperationException("boom"));

        Assert.False(result);
        session.Verify(x => x.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
        session.Verify(x => x.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        session.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExecuteUnderTransactionWithExceptionAsync_WhenMongoCommandInInnerException_ReturnsMongoException()
    {
        var session = new Mock<IClientSessionHandle>();
        var mongoException = CreateMongoCommandException();

        var result = await MongoTransactionHelper.ExecuteUnderTransactionWithExceptionAsync(
            session.Object,
            () => throw new Exception("outer", mongoException));

        Assert.False(result.Item1);
        Assert.NotNull(result.Item2);
        session.Verify(x => x.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
        session.Verify(x => x.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteUnderTransactionWithExceptionAsync_WhenActionSucceeds_ReturnsSuccess()
    {
        var session = new Mock<IClientSessionHandle>();

        var result = await MongoTransactionHelper.ExecuteUnderTransactionWithExceptionAsync(
            session.Object,
            () => Task.CompletedTask);

        Assert.True(result.Item1);
        Assert.Null(result.Item2);
        session.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void BaseMappingProfile_WhenConfigured_IsValidAndMapsDtos()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BaseMappingProfile>(), new NullLoggerFactory());
        config.AssertConfigurationIsValid();
        var mapper = config.CreateMapper();

        var identityUser = new Defender.Common.Clients.Identity.UserDto
        {
            Id = Guid.NewGuid(),
            Email = "e@e.com",
            PhoneNumber = "123",
            Nickname = "nick",
            CreatedDate = DateTime.UtcNow
        };
        var identityAccount = new Defender.Common.Clients.Identity.AccountDto
        {
            Id = Guid.NewGuid(),
            Roles = new List<string> { "Admin" },
            IsBlocked = true,
            IsEmailVerified = true,
            IsPhoneVerified = false
        };
        var paged = new PagedResult<Defender.Common.Clients.Identity.UserDto>
        {
            Items = new List<Defender.Common.Clients.Identity.UserDto> { identityUser },
            CurrentPage = 1,
            PageSize = 5,
            TotalItemsCount = 1
        };

        var mappedUser = mapper.Map<Defender.Common.DTOs.UserDto>(identityUser);
        var mappedAccount = mapper.Map<Defender.Common.DTOs.AccountDto>(identityAccount);
        var mappedPaged = mapper.Map<PagedResult<Defender.Common.DTOs.UserDto>>(paged);

        Assert.Equal(identityUser.Email, mappedUser.Email);
        Assert.Equal(identityAccount.Id, mappedAccount.Id);
        Assert.True(mappedAccount.IsBlocked);
        Assert.Single(mappedPaged.Items);
    }

    [Fact]
    public void FluentValidationWithMessage_WhenUsingErrorCode_SetsExpectedMessage()
    {
        var validator = new SampleValidator();
        var result = validator.Validate(new SampleRequest(string.Empty));

        Assert.False(result.IsValid);
        Assert.Equal(nameof(ErrorCode.CM_InvalidUserJWT), result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task GetConfigurationQueryHandler_WhenLevelAll_ReturnsProcessEnvironmentValue()
    {
        var key = $"UNIT_TEST_ENV_{Guid.NewGuid():N}";
        var value = "value-from-process";
        Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
        var handler = new GetConfigurationQueryHandler();

        var result = await handler.Handle(new GetConfigurationQuery { Level = Defender.Common.Enums.ConfigurationLevel.All }, CancellationToken.None);

        Assert.True(result.ContainsKey(key));
        Assert.Equal(value, result[key]);
    }

    private static MongoCommandException CreateMongoCommandException()
    {
        var clusterId = new ClusterId();
        var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
        var connectionId = new ConnectionId(serverId);
        return new MongoCommandException(connectionId, "Command failed", new BsonDocument("ok", 0));
    }

    private sealed record SampleRequest(string Name);

    private sealed class SampleValidator : AbstractValidator<SampleRequest>
    {
        public SampleValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ErrorCode.CM_InvalidUserJWT);
        }
    }
}
