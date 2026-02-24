using Defender.UserManagementService.Application.Models;
using Defender.UserManagementService.Domain.Entities;

namespace Defender.UserManagementService.Tests.Models;

public class UpdateUserInfoRequestTests
{
    [Fact]
    public void ToUserInfo_WhenCalled_MapsAllProperties()
    {
        var id = Guid.NewGuid();
        var request = new UpdateUserInfoRequest { Id = id, Email = "e@b.com", PhoneNumber = "p", Nickname = "n" };

        var userInfo = request.ToUserInfo();

        Assert.Equal(id, userInfo.Id);
        Assert.Equal("e@b.com", userInfo.Email);
        Assert.Equal("p", userInfo.PhoneNumber);
        Assert.Equal("n", userInfo.Nickname);
    }

    [Fact]
    public void AsUser_WhenCalled_NullsEmailAndPhoneNumber()
    {
        var request = new UpdateUserInfoRequest { Id = Guid.NewGuid(), Email = "e@b.com", PhoneNumber = "p", Nickname = "n" };

        var result = request.AsUser();

        Assert.Same(request, result);
        Assert.Null(request.Email);
        Assert.Null(request.PhoneNumber);
        Assert.Equal("n", request.Nickname);
    }
}
