using Defender.BudgetTracker.Application.Common.Interfaces.Repositories;
using Defender.BudgetTracker.Application.Models.Groups;
using Defender.BudgetTracker.Application.Services;
using Defender.BudgetTracker.Domain.Entities.Groups;
using Defender.Common.DB.Pagination;
using Defender.Common.Interfaces;

namespace Defender.BudgetTracker.Tests.Services;

public class GroupServiceTests
{
    private readonly Mock<IGroupRepository> _groupRepository = new();
    private readonly Mock<ICurrentAccountAccessor> _currentAccountAccessor = new();

    [Fact]
    public async Task GetCurrentUserGroupsAsync_WhenCalled_UsesCurrentUserId()
    {
        var userId = Guid.NewGuid();
        var paginationRequest = new PaginationRequest();
        var expected = new PagedResult<Group> { Items = [new Group()] };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _groupRepository
            .Setup(x => x.GetGroupsAsync(paginationRequest, userId))
            .ReturnsAsync(expected);
        var sut = new GroupService(_groupRepository.Object, _currentAccountAccessor.Object);

        var result = await sut.GetCurrentUserGroupsAsync(paginationRequest);

        Assert.Same(expected, result);
        _groupRepository.Verify(x => x.GetGroupsAsync(paginationRequest, userId), Times.Once);
    }

    [Fact]
    public async Task CreateGroupAsync_WhenCalled_AssignsCurrentUserId()
    {
        var userId = Guid.NewGuid();
        var request = new CreateGroupRequest
        {
            Name = "Main",
            IsActive = true,
            Tags = ["a", "b"],
            MainColor = "#fff",
            ShowTrendLine = true,
            TrendLineColor = "#000"
        };
        _currentAccountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        _groupRepository
            .Setup(x => x.CreateGroupAsync(It.IsAny<Group>()))
            .ReturnsAsync((Group group) => group);
        var sut = new GroupService(_groupRepository.Object, _currentAccountAccessor.Object);

        var result = await sut.CreateGroupAsync(request);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(request.Name, result.Name);
        _groupRepository.Verify(x => x.CreateGroupAsync(It.IsAny<Group>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGroupAsync_WhenCalled_DelegatesToRepository()
    {
        var groupId = Guid.NewGuid();
        var request = new UpdateGroupRequest
        {
            Id = groupId,
            Name = "Updated Group",
            IsActive = false,
            MainColor = "#ccc"
        };
        var updated = new Group { Id = groupId, Name = "Updated Group", IsActive = false };
        _groupRepository
            .Setup(x => x.UpdateGroupAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Group>>()))
            .ReturnsAsync(updated);
        var sut = new GroupService(_groupRepository.Object, _currentAccountAccessor.Object);

        var result = await sut.UpdateGroupAsync(request);

        Assert.Equal("Updated Group", result.Name);
        Assert.False(result.IsActive);
        _groupRepository.Verify(x => x.UpdateGroupAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<Group>>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupAsync_WhenCalled_DeletesAndReturnsId()
    {
        var groupId = Guid.NewGuid();
        _groupRepository.Setup(x => x.DeleteGroupAsync(groupId)).Returns(Task.CompletedTask);
        var sut = new GroupService(_groupRepository.Object, _currentAccountAccessor.Object);

        var result = await sut.DeleteGroupAsync(groupId);

        Assert.Equal(groupId, result);
        _groupRepository.Verify(x => x.DeleteGroupAsync(groupId), Times.Once);
    }
}
