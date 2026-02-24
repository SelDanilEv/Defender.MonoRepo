using Defender.UserManagementService.Application.DTOs;

namespace Defender.UserManagementService.Tests.DTOs;

public class PublicUserInfoDtoTests
{
    [Fact]
    public void Properties_WhenSet_RoundTripValues()
    {
        var id = Guid.NewGuid();
        var dto = new PublicUserInfoDto
        {
            Id = id,
            Nickname = "nick"
        };

        Assert.Equal(id, dto.Id);
        Assert.Equal("nick", dto.Nickname);
    }
}
