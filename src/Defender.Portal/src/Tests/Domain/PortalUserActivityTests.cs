using Defender.Portal.Domain.Entities;
using Defender.Portal.Domain.Enums;

namespace Defender.Portal.Tests.Domain;

public class PortalUserActivityTests
{
    [Fact]
    public void Create_WhenMessageProvided_SetsUserCodeAndMessage()
    {
        var userId = Guid.NewGuid();

        var activity = PortalUserActivity.Create(userId, ActivityCode.CreateUserWithPassword, "created");

        Assert.Equal(userId, activity.UserId);
        Assert.Equal(ActivityCode.CreateUserWithPassword, activity.Code);
        Assert.Equal("created", activity.Message);
    }

    [Fact]
    public void Create_WhenMessageIsNull_UsesEmptyString()
    {
        var activity = PortalUserActivity.Create(Guid.NewGuid(), ActivityCode.CreateUserWithPassword, null);

        Assert.Equal(string.Empty, activity.Message);
    }
}
