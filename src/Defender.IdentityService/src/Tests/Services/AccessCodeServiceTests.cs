using Defender.Common.Exceptions;
using Defender.IdentityService.Application.Common.Interfaces.Repositories;
using Defender.IdentityService.Application.Services;
using Defender.IdentityService.Domain.Entities;
using Defender.IdentityService.Domain.Enum;

namespace Defender.IdentityService.Tests.Services;

public class AccessCodeServiceTests
{
    private readonly Mock<IAccessCodeRepository> _accessCodeRepository = new();

    [Fact]
    public async Task VerifyAccessCode_WhenCodeNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _accessCodeRepository
            .Setup(x => x.GetAccessCodeByUserIdAsync(userId, 123456))
            .ReturnsAsync((AccessCode)null!);
        var sut = new AccessCodeService(_accessCodeRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.VerifyAccessCode(userId, 123456, AccessCodeType.EmailVerification));
    }

    [Fact]
    public async Task VerifyAccessCode_WhenCodeTypeMismatched_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var accessCode = AccessCode.CreateAccessCode(userId, AccessCodeType.ResetPassword);
        _accessCodeRepository
            .Setup(x => x.GetAccessCodeByUserIdAsync(userId, 654321))
            .ReturnsAsync(accessCode);
        var sut = new AccessCodeService(_accessCodeRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.VerifyAccessCode(userId, 654321, AccessCodeType.EmailVerification));
    }

    [Fact]
    public async Task VerifyAccessCode_WhenActiveAndValid_MarksAsUsedAndReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var accessCode = AccessCode.CreateAccessCode(userId, AccessCodeType.EmailVerification, validTimeMinutes: 30);
        accessCode.CreatedDate = DateTime.UtcNow;
        _accessCodeRepository
            .Setup(x => x.GetAccessCodeByUserIdAsync(userId, 111111))
            .ReturnsAsync(accessCode);
        _accessCodeRepository
            .Setup(x => x.UpdateAccessCodeAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<AccessCode>>()))
            .ReturnsAsync(new AccessCode(accessCode.UserId)
            {
                Status = AccessCodeStatus.Used,
                Type = accessCode.Type,
                CreatedDate = accessCode.CreatedDate,
                ValidTime = accessCode.ValidTime,
                Code = accessCode.Code,
                Hash = accessCode.Hash
            });
        var sut = new AccessCodeService(_accessCodeRepository.Object);

        var result = await sut.VerifyAccessCode(userId, 111111, AccessCodeType.EmailVerification);

        Assert.True(result);
        _accessCodeRepository.Verify(
            x => x.UpdateAccessCodeAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<AccessCode>>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyAccessCode_WhenAlreadyUsed_ThrowsServiceException()
    {
        var userId = Guid.NewGuid();
        var accessCode = AccessCode.CreateAccessCode(userId, AccessCodeType.EmailVerification);
        accessCode.Status = AccessCodeStatus.Used;
        _accessCodeRepository
            .Setup(x => x.GetAccessCodeByUserIdAsync(userId, 222222))
            .ReturnsAsync(accessCode);
        var sut = new AccessCodeService(_accessCodeRepository.Object);

        await Assert.ThrowsAsync<ServiceException>(
            () => sut.VerifyAccessCode(userId, 222222, AccessCodeType.EmailVerification));
    }
}
