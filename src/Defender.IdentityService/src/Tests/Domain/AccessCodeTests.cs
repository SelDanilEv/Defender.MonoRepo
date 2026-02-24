using Defender.IdentityService.Domain.Entities;
using Defender.IdentityService.Domain.Enum;

namespace Defender.IdentityService.Tests.Domain;

public class AccessCodeTests
{
    [Fact]
    public void CreateAccessCode_WhenCalled_SetsUserTypeAndValidTime()
    {
        var userId = Guid.NewGuid();

        var accessCode = AccessCode.CreateAccessCode(userId, AccessCodeType.EmailVerification, 30);

        Assert.Equal(userId, accessCode.UserId);
        Assert.Equal(AccessCodeType.EmailVerification, accessCode.Type);
        Assert.Equal(TimeSpan.FromMinutes(30), accessCode.ValidTime);
    }

    [Fact]
    public void IsExpired_WhenExpirationDateInPast_ReturnsTrue()
    {
        var accessCode = new AccessCode(Guid.NewGuid())
        {
            CreatedDate = DateTime.UtcNow.AddMinutes(-20),
            ValidTime = TimeSpan.FromMinutes(10),
        };

        Assert.True(accessCode.IsExpired);
    }

    [Fact]
    public void IsExpired_WhenExpirationDateInFuture_ReturnsFalse()
    {
        var accessCode = new AccessCode(Guid.NewGuid())
        {
            CreatedDate = DateTime.UtcNow,
            ValidTime = TimeSpan.FromMinutes(10),
        };

        Assert.False(accessCode.IsExpired);
    }
}
