using AutoMapper;
using Defender.Common.DTOs;
using Defender.UserManagementService.Application.DTOs;
using Defender.UserManagementService.Application.Mappings;
using Defender.UserManagementService.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defender.UserManagementService.Tests.Infrastructure.Mappings;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), new NullLoggerFactory());
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Config_WhenCreated_IsValid()
    {
        Assert.NotNull(_mapper);
    }

    [Fact]
    public void Map_UserInfoToPublicUserInfoDto_MapsIdAndNickname()
    {
        var user = new UserInfo
        {
            Id = Guid.NewGuid(),
            Nickname = "nickname"
        };

        var dto = _mapper.Map<PublicUserInfoDto>(user);

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Nickname, dto.Nickname);
    }

    [Fact]
    public void Map_UserInfoToUserDto_MapsCoreProperties()
    {
        var createdDate = DateTime.UtcNow;
        var user = new UserInfo
        {
            Id = Guid.NewGuid(),
            Email = "mail@example.com",
            PhoneNumber = "+1000000000",
            Nickname = "nickname",
            CreatedDate = createdDate
        };

        var dto = _mapper.Map<UserDto>(user);

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal(user.PhoneNumber, dto.PhoneNumber);
        Assert.Equal(user.Nickname, dto.Nickname);
        Assert.Equal(createdDate, dto.CreatedDate);
    }
}
