using Defender.Common.DB.Model;
using Defender.Common.DB.Pagination;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.UserManagementService.Application.Common.Interfaces.Repositories;
using Defender.UserManagementService.Application.Common.Interfaces.Wrappers;
using Defender.UserManagementService.Application.Models;
using Defender.UserManagementService.Application.Services;
using Defender.UserManagementService.Domain.Entities;

namespace Defender.UserManagementService.Tests.Services;

public class UserManagementServiceTests
{
    private readonly Mock<IUserInfoRepository> _repository;
    private readonly Mock<IIdentityWrapper> _identityWrapper;
    private readonly Application.Services.UserManagementService _sut;

    public UserManagementServiceTests()
    {
        _repository = new Mock<IUserInfoRepository>();
        _identityWrapper = new Mock<IIdentityWrapper>();
        _sut = new Application.Services.UserManagementService(_repository.Object, _identityWrapper.Object);
    }

    [Fact]
    public async Task GetUsersAsync_WhenCalled_ReturnsRepositoryResult()
    {
        var request = new PaginationRequest { Page = 1, PageSize = 10 };
        var expected = new PagedResult<UserInfo> { TotalItemsCount = 2, CurrentPage = 1, PageSize = 10, Items = [] };
        _repository.Setup(r => r.GetUsersAsync(request)).ReturnsAsync(expected);

        var result = await _sut.GetUsersAsync(request);

        Assert.Same(expected, result);
        _repository.Verify(r => r.GetUsersAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenCalled_ReturnsRepositoryResult()
    {
        var userId = Guid.NewGuid();
        var expected = new UserInfo { Id = userId, Email = "a@b.com" };
        _repository.Setup(r => r.GetUserInfoByIdAsync(userId)).ReturnsAsync(expected);

        var result = await _sut.GetUserByIdAsync(userId);

        Assert.Same(expected, result);
        _repository.Verify(r => r.GetUserInfoByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserByLoginAsync_WhenCalled_ReturnsRepositoryResult()
    {
        var login = "user@test.com";
        var expected = new UserInfo { Email = login };
        _repository.Setup(r => r.GetUserInfoByLoginAsync(login)).ReturnsAsync(expected);

        var result = await _sut.GetUserByLoginAsync(login);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task CheckIfEmailTakenAsync_WhenCalled_ReturnsRepositoryResult()
    {
        _repository.Setup(r => r.CheckIfEmailTakenAsync("x@y.com")).ReturnsAsync(true);

        var result = await _sut.CheckIfEmailTakenAsync("x@y.com");

        Assert.True(result);
    }

    [Fact]
    public async Task CreateUserAsync_WhenNoConflicts_CreatesAndReturnsUser()
    {
        _repository.Setup(r => r.GetUsersInfoByAllFieldsAsync(It.IsAny<UserInfo>())).ReturnsAsync(new List<UserInfo>());
        UserInfo? captured = null;
        _repository.Setup(r => r.CreateUserInfoAsync(It.IsAny<UserInfo>()))
            .Callback<UserInfo>(u => captured = u)
            .ReturnsAsync((UserInfo u) => u);

        var result = await _sut.CreateUserAsync("a@b.com", "+123", "nick");

        Assert.NotNull(result);
        Assert.Equal("a@b.com", result.Email);
        Assert.Equal("+123", result.PhoneNumber);
        Assert.Equal("nick", result.Nickname);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotNull(result.CreatedDate);
        Assert.NotNull(captured);
        _repository.Verify(r => r.CreateUserInfoAsync(It.IsAny<UserInfo>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailInUse_ThrowsServiceException()
    {
        var existing = new UserInfo { Id = Guid.NewGuid(), Email = "a@b.com" };
        _repository.Setup(r => r.GetUsersInfoByAllFieldsAsync(It.IsAny<UserInfo>())).ReturnsAsync(new List<UserInfo> { existing });

        var ex = await Assert.ThrowsAsync<ServiceException>(
            () => _sut.CreateUserAsync("a@b.com", null!, "nick"));

        Assert.True(ex.IsErrorCode(ErrorCode.BR_USM_EmailAddressInUse));
        _repository.Verify(r => r.CreateUserInfoAsync(It.IsAny<UserInfo>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WhenPhoneInUse_ThrowsServiceException()
    {
        var existing = new UserInfo { Id = Guid.NewGuid(), PhoneNumber = "+123" };
        _repository.Setup(r => r.GetUsersInfoByAllFieldsAsync(It.IsAny<UserInfo>())).ReturnsAsync(new List<UserInfo> { existing });

        var ex = await Assert.ThrowsAsync<ServiceException>(
            () => _sut.CreateUserAsync("other@b.com", "+123", "nick"));

        Assert.True(ex.IsErrorCode(ErrorCode.BR_USM_PhoneNumberInUse));
        _repository.Verify(r => r.CreateUserInfoAsync(It.IsAny<UserInfo>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WhenNicknameInUse_ThrowsServiceException()
    {
        var existing = new UserInfo { Id = Guid.NewGuid(), Nickname = "nick" };
        _repository.Setup(r => r.GetUsersInfoByAllFieldsAsync(It.IsAny<UserInfo>())).ReturnsAsync(new List<UserInfo> { existing });

        var ex = await Assert.ThrowsAsync<ServiceException>(
            () => _sut.CreateUserAsync("other@b.com", null!, "nick"));

        Assert.True(ex.IsErrorCode(ErrorCode.BR_USM_NicknameInUse));
        _repository.Verify(r => r.CreateUserInfoAsync(It.IsAny<UserInfo>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenOnlyExistingUserIsSelf_ExcludesSelfAndSucceeds()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserInfoRequest { Id = userId, Email = "same@b.com", Nickname = "sameNick" };
        var self = new UserInfo { Id = userId, Email = "same@b.com", Nickname = "sameNick" };
        _repository.Setup(r => r.GetUsersInfoByAllFieldsAsync(It.IsAny<UserInfo>())).ReturnsAsync(new List<UserInfo> { self });
        _repository.Setup(r => r.UpdateUserInfoAsync(It.IsAny<UpdateModelRequest<UserInfo>>()))
            .ReturnsAsync(new UserInfo { Id = userId, Email = "same@b.com", Nickname = "sameNick" });

        var result = await _sut.UpdateUserAsync(request);

        Assert.NotNull(result);
        _repository.Verify(r => r.UpdateUserInfoAsync(It.IsAny<UpdateModelRequest<UserInfo>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNoConflicts_UpdatesAndReturnsUser()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserInfoRequest { Id = userId, Nickname = "newnick" };
        var updated = new UserInfo { Id = userId, Nickname = "newnick" };
        _repository.Setup(r => r.GetUsersInfoByAllFieldsAsync(It.IsAny<UserInfo>())).ReturnsAsync(new List<UserInfo>());
        _repository.Setup(r => r.UpdateUserInfoAsync(It.IsAny<UpdateModelRequest<UserInfo>>())).ReturnsAsync(updated);

        var result = await _sut.UpdateUserAsync(request);

        Assert.Same(updated, result);
        _repository.Verify(r => r.UpdateUserInfoAsync(It.IsAny<UpdateModelRequest<UserInfo>>()), Times.Once);
        _identityWrapper.Verify(i => i.UpdateAccountVerificationAsync(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenEmailProvided_CallsIdentityWrapper()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserInfoRequest { Id = userId, Email = "new@b.com" };
        var updated = new UserInfo { Id = userId, Email = "new@b.com" };
        _repository.Setup(r => r.GetUsersInfoByAllFieldsAsync(It.IsAny<UserInfo>())).ReturnsAsync(new List<UserInfo>());
        _repository.Setup(r => r.UpdateUserInfoAsync(It.IsAny<UpdateModelRequest<UserInfo>>())).ReturnsAsync(updated);

        await _sut.UpdateUserAsync(request);

        _identityWrapper.Verify(i => i.UpdateAccountVerificationAsync(userId, false), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenEmailInUseByOther_ThrowsServiceException()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserInfoRequest { Id = userId, Email = "taken@b.com" };
        var other = new UserInfo { Id = Guid.NewGuid(), Email = "taken@b.com" };
        _repository.Setup(r => r.GetUsersInfoByAllFieldsAsync(It.IsAny<UserInfo>())).ReturnsAsync(new List<UserInfo> { other });

        var ex = await Assert.ThrowsAsync<ServiceException>(() => _sut.UpdateUserAsync(request));

        Assert.True(ex.IsErrorCode(ErrorCode.BR_USM_EmailAddressInUse));
        _repository.Verify(r => r.UpdateUserInfoAsync(It.IsAny<UpdateModelRequest<UserInfo>>()), Times.Never);
    }

    [Fact]
    public void CreateUpdateRequest_WhenAllFieldsSet_IncludesAllInRequest()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserInfoRequest { Id = userId, Email = "e@b.com", PhoneNumber = "p", Nickname = "n" };

        var updateRequest = _sut.CreateUpdateRequest(request);

        Assert.Equal(userId, updateRequest.ModelId);
    }

    [Fact]
    public void CreateUpdateRequest_WhenOnlyNicknameSet_DoesNotThrow()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserInfoRequest { Id = userId, Nickname = "only" };

        var updateRequest = _sut.CreateUpdateRequest(request);

        Assert.Equal(userId, updateRequest.ModelId);
    }
}
