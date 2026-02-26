using Defender.BudgetTracker.Application.Common.Interfaces.Services;
using Defender.BudgetTracker.Application.Modules.Groups.Commands;
using Defender.BudgetTracker.Application.Modules.Groups.Queries;
using Defender.BudgetTracker.Domain.Entities.Groups;
using Defender.Common.DB.Pagination;
using Moq;

namespace Defender.BudgetTracker.Tests.Handlers;

public class GroupHandlersTests
{
    private readonly Mock<IGroupService> _groupService = new();

    [Fact]
    public async Task GetGroupsQueryHandler_WhenCalled_DelegatesToService()
    {
        var request = new GetGroupsQuery();
        var expected = new PagedResult<Group> { Items = [new Group()] };
        _groupService.Setup(x => x.GetCurrentUserGroupsAsync(request)).ReturnsAsync(expected);

        var handler = new GetGroupsQueryHandler(_groupService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _groupService.Verify(x => x.GetCurrentUserGroupsAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreateGroupCommandHandler_WhenCalled_DelegatesToService()
    {
        var request = new CreateGroupCommand { Name = "G1", IsActive = true };
        var expected = new Group { Name = "G1" };
        _groupService.Setup(x => x.CreateGroupAsync(request)).ReturnsAsync(expected);

        var handler = new CreateGroupCommandHandler(_groupService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _groupService.Verify(x => x.CreateGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateGroupCommandHandler_WhenCalled_DelegatesToService()
    {
        var groupId = Guid.NewGuid();
        var request = new UpdateGroupCommand { Id = groupId, Name = "Updated" };
        var expected = new Group { Id = groupId, Name = "Updated" };
        _groupService.Setup(x => x.UpdateGroupAsync(request)).ReturnsAsync(expected);

        var handler = new UpdateGroupCommandHandler(_groupService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
        _groupService.Verify(x => x.UpdateGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupCommandHandler_WhenCalled_DelegatesToService()
    {
        var groupId = Guid.NewGuid();
        var request = new DeleteGroupCommand { Id = groupId };
        _groupService.Setup(x => x.DeleteGroupAsync(groupId)).ReturnsAsync(groupId);

        var handler = new DeleteGroupCommandHandler(_groupService.Object);
        var result = await handler.Handle(request, CancellationToken.None);

        Assert.Equal(groupId, result);
        _groupService.Verify(x => x.DeleteGroupAsync(groupId), Times.Once);
    }
}
