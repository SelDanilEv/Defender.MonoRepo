using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Defender.PersonalFoodAdviser.Application.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Defender.PersonalFoodAdviser.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Defender.PersonalFoodAdviser.Tests;

public class MenuParsingProcessorTests
{
    private readonly Mock<IImageBlobRepository> _imageBlobRepo;
    private readonly Mock<IMenuSessionRepository> _menuSessionRepo;
    private readonly Mock<IHuggingFaceClient> _huggingFaceClient;
    private readonly Mock<ILogger<MenuParsingProcessor>> _logger;
    private readonly MenuParsingProcessor _sut;

    public MenuParsingProcessorTests()
    {
        _imageBlobRepo = new Mock<IImageBlobRepository>();
        _menuSessionRepo = new Mock<IMenuSessionRepository>();
        _huggingFaceClient = new Mock<IHuggingFaceClient>();
        _logger = new Mock<ILogger<MenuParsingProcessor>>();
        _sut = new MenuParsingProcessor(
            _imageBlobRepo.Object,
            _menuSessionRepo.Object,
            _huggingFaceClient.Object,
            _logger.Object);
    }

    [Fact]
    public async Task ProcessAsync_WhenSessionNotFound_DoesNotUpdateSession()
    {
        var sessionId = Guid.NewGuid();
        var evt = new MenuParsingRequestedEvent(sessionId, Guid.NewGuid(), ["any-ref"]);

        _menuSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync((MenuSession?)null);

        await _sut.ProcessAsync(evt);

        _menuSessionRepo.Verify(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenImageRefIsNotValidGuid_SkipsRefAndResolvesZeroImages_ThenSetsFailed()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var session = new MenuSession { Id = sessionId, UserId = userId, ImageRefs = ["not-a-guid"], Status = MenuSessionStatus.Uploaded };
        var evt = new MenuParsingRequestedEvent(sessionId, userId, ["not-a-guid"]);

        _menuSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _menuSessionRepo.Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);

        await _sut.ProcessAsync(evt);

        _imageBlobRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _huggingFaceClient.Verify(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()), Times.Never);
        _menuSessionRepo.Verify(r => r.UpdateAsync(It.Is<MenuSession>(s => s.Status == MenuSessionStatus.Failed), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_WhenBlobNotFoundForValidRef_ResolvesZeroImages_ThenSetsFailed()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var blobId = Guid.NewGuid();
        var session = new MenuSession { Id = sessionId, UserId = userId, ImageRefs = [blobId.ToString()], Status = MenuSessionStatus.Uploaded };
        var evt = new MenuParsingRequestedEvent(sessionId, userId, [blobId.ToString()]);

        _menuSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _menuSessionRepo.Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _imageBlobRepo.Setup(r => r.GetByIdAsync(blobId, It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.ImageBlob?)null);

        await _sut.ProcessAsync(evt);

        _huggingFaceClient.Verify(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()), Times.Never);
        _menuSessionRepo.Verify(r => r.UpdateAsync(It.Is<MenuSession>(s => s.Status == MenuSessionStatus.Failed), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_WhenBlobsLoadedAndHuggingFaceReturnsDishes_SetsParsedItemsAndReview()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var blobId = Guid.NewGuid();
        var session = new MenuSession { Id = sessionId, UserId = userId, ImageRefs = [blobId.ToString()], Status = MenuSessionStatus.Uploaded };
        var evt = new MenuParsingRequestedEvent(sessionId, userId, [blobId.ToString()]);
        var imageData = new byte[] { 0xFF, 0xD8 };
        var blob = new Domain.Entities.ImageBlob { Id = blobId, Data = imageData };
        var expectedDishes = new List<string> { "Caesar Salad", "Grilled Salmon" };

        _menuSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _menuSessionRepo.Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _imageBlobRepo.Setup(r => r.GetByIdAsync(blobId, It.IsAny<CancellationToken>())).ReturnsAsync(blob);
        _huggingFaceClient.Setup(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDishes);

        await _sut.ProcessAsync(evt);

        _menuSessionRepo.Verify(r => r.UpdateAsync(It.Is<MenuSession>(s =>
            s.Status == MenuSessionStatus.Review &&
            s.ParsedItems.Count == 2 &&
            s.ParsedItems[0] == "Caesar Salad" &&
            s.ParsedItems[1] == "Grilled Salmon"), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_WhenBlobsLoadedButHuggingFaceReturnsEmpty_SetsParsedItemsEmptyAndReview()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var blobId = Guid.NewGuid();
        var session = new MenuSession { Id = sessionId, UserId = userId, ImageRefs = [blobId.ToString()], Status = MenuSessionStatus.Uploaded };
        var evt = new MenuParsingRequestedEvent(sessionId, userId, [blobId.ToString()]);
        var blob = new Domain.Entities.ImageBlob { Id = blobId, Data = new byte[] { 0xFF, 0xD8 } };

        _menuSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _menuSessionRepo.Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _imageBlobRepo.Setup(r => r.GetByIdAsync(blobId, It.IsAny<CancellationToken>())).ReturnsAsync(blob);
        _huggingFaceClient.Setup(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        await _sut.ProcessAsync(evt);

        _menuSessionRepo.Verify(r => r.UpdateAsync(It.Is<MenuSession>(s =>
            s.Status == MenuSessionStatus.Review && s.ParsedItems.Count == 0), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
