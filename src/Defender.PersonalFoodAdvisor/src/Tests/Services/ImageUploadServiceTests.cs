using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Application.Services;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Defender.PersonalFoodAdvisor.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Defender.PersonalFoodAdvisor.Tests.Services;

public class ImageUploadServiceTests
{
    private readonly Mock<IImageBlobRepository> _imageBlobRepository = new();
    private readonly Mock<IMenuSessionRepository> _menuSessionRepository = new();
    private readonly Mock<ILogger<ImageUploadService>> _logger = new();

    private ImageUploadService CreateSut()
        => new(_imageBlobRepository.Object, _menuSessionRepository.Object, _logger.Object);

    [Fact]
    public async Task UploadAsync_WhenSessionExists_AppendsRefsAndResetsDerivedState()
    {
        var sessionId = Guid.NewGuid();
        var existingSession = new MenuSession
        {
            Id = sessionId,
            Status = MenuSessionStatus.Confirmed,
            ImageRefs = ["existing-ref"],
            ParsedItems = ["Pasta"],
            ConfirmedItems = ["Pasta"],
            RankedItems = ["Pasta"],
            TrySomethingNew = true
        };
        var savedBlobIds = new Queue<Guid>([Guid.NewGuid(), Guid.NewGuid()]);
        var savedBlobs = new List<ImageBlob>();

        _imageBlobRepository
            .Setup(r => r.SaveAsync(It.IsAny<ImageBlob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImageBlob blob, CancellationToken _) =>
            {
                savedBlobs.Add(new ImageBlob
                {
                    Id = blob.Id,
                    SessionId = blob.SessionId,
                    Data = blob.Data,
                    ContentType = blob.ContentType,
                    ImageHash = blob.ImageHash
                });
                blob.Id = savedBlobIds.Dequeue();
                return blob;
            });
        _menuSessionRepository
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSession);
        _menuSessionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MenuSession session, CancellationToken _) => session);

        var sut = CreateSut();
        await using var first = new MemoryStream([0x01, 0x02]);
        await using var second = new MemoryStream([0x03, 0x04]);

        var result = await sut.UploadAsync(
            sessionId,
            [(first, "image/jpeg"), (second, "image/png")]);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, savedBlobs.Count);
        Assert.Equal(Convert.ToHexString(SHA256.HashData([0x01, 0x02])), savedBlobs[0].ImageHash);
        Assert.Equal(Convert.ToHexString(SHA256.HashData([0x03, 0x04])), savedBlobs[1].ImageHash);
        _menuSessionRepository.Verify(r => r.UpdateAsync(
            It.Is<MenuSession>(s =>
                s.Status == MenuSessionStatus.Uploaded &&
                s.ImageRefs.Count == 3 &&
                s.ParsedItems.Count == 0 &&
                s.ConfirmedItems.Count == 0 &&
                s.RankedItems.Count == 0 &&
                !s.TrySomethingNew),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
