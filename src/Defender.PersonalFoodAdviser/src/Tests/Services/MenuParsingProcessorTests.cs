using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdviser.Application.Common.Interfaces.Services;
using Defender.PersonalFoodAdviser.Application.Kafka;
using Defender.PersonalFoodAdviser.Application.Services;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Defender.PersonalFoodAdviser.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Defender.PersonalFoodAdviser.Tests.Services;

public class MenuParsingProcessorTests
{
    private readonly Mock<IImageBlobRepository> _imageBlobRepo;
    private readonly Mock<IMenuSessionRepository> _menuSessionRepo;
    private readonly Mock<IMenuIntelligenceClient> _menuIntelligenceClient;
    private readonly Mock<ILogger<MenuParsingProcessor>> _logger;
    private readonly MenuParsingProcessor _sut;

    public MenuParsingProcessorTests()
    {
        _imageBlobRepo = new Mock<IImageBlobRepository>();
        _menuSessionRepo = new Mock<IMenuSessionRepository>();
        _menuIntelligenceClient = new Mock<IMenuIntelligenceClient>();
        _logger = new Mock<ILogger<MenuParsingProcessor>>();
        _sut = new MenuParsingProcessor(
            _imageBlobRepo.Object,
            _menuSessionRepo.Object,
            _menuIntelligenceClient.Object,
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
        _menuIntelligenceClient.Verify(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()), Times.Never);
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

        _menuIntelligenceClient.Verify(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()), Times.Never);
        _menuSessionRepo.Verify(r => r.UpdateAsync(It.Is<MenuSession>(s => s.Status == MenuSessionStatus.Failed), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_WhenBlobsLoadedAndClientReturnsDishes_SetsParsedItemsAndReview()
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
        _imageBlobRepo
            .Setup(r => r.FindSessionIdsByExactImageHashesAsync(It.IsAny<IReadOnlyList<string>>(), sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _menuIntelligenceClient.Setup(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDishes);

        await _sut.ProcessAsync(evt);

        _menuSessionRepo.Verify(r => r.UpdateAsync(It.Is<MenuSession>(s =>
            s.Status == MenuSessionStatus.Review &&
            s.ParsedItems.Count == 2 &&
            s.ParsedItems[0] == "Caesar Salad" &&
            s.ParsedItems[1] == "Grilled Salmon"), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_WhenBlobsLoadedButClientReturnsEmpty_SetsParsedItemsEmptyAndReview()
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
        _imageBlobRepo
            .Setup(r => r.FindSessionIdsByExactImageHashesAsync(It.IsAny<IReadOnlyList<string>>(), sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _menuIntelligenceClient.Setup(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        await _sut.ProcessAsync(evt);

        _menuSessionRepo.Verify(r => r.UpdateAsync(It.Is<MenuSession>(s =>
            s.Status == MenuSessionStatus.Review && s.ParsedItems.Count == 0), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_WhenEventUserDoesNotOwnSession_DoesNotProcess()
    {
        var sessionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var evt = new MenuParsingRequestedEvent(sessionId, Guid.NewGuid(), ["any-ref"]);

        _menuSessionRepo
            .Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MenuSession { Id = sessionId, UserId = ownerId, Status = MenuSessionStatus.Uploaded });

        await _sut.ProcessAsync(evt);

        _imageBlobRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _menuIntelligenceClient.Verify(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()), Times.Never);
        _menuSessionRepo.Verify(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenEventRefsAreStale_UsesPersistedSessionRefs()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var persistedBlobId = Guid.NewGuid();
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            Status = MenuSessionStatus.Uploaded,
            ImageRefs = [persistedBlobId.ToString()]
        };
        var evt = new MenuParsingRequestedEvent(sessionId, userId, ["stale-ref"]);
        var blob = new Domain.Entities.ImageBlob { Id = persistedBlobId, Data = [0x01, 0x02] };

        _menuSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _menuSessionRepo.Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>())).ReturnsAsync((MenuSession s, CancellationToken _) => s);
        _imageBlobRepo.Setup(r => r.GetByIdAsync(persistedBlobId, It.IsAny<CancellationToken>())).ReturnsAsync(blob);
        _imageBlobRepo
            .Setup(r => r.FindSessionIdsByExactImageHashesAsync(It.IsAny<IReadOnlyList<string>>(), sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _menuIntelligenceClient
            .Setup(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Pho"]);
        var expectedParsedItems = new[] { "Pho" };

        await _sut.ProcessAsync(evt);

        _imageBlobRepo.Verify(r => r.GetByIdAsync(persistedBlobId, It.IsAny<CancellationToken>()), Times.Once);
        _menuSessionRepo.Verify(r => r.UpdateAsync(It.Is<MenuSession>(s =>
            s.Status == MenuSessionStatus.Review &&
            s.ParsedItems.SequenceEqual(expectedParsedItems)), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_WhenSessionAlreadyReviewedForCurrentImages_SkipsDuplicateWork()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var blobId = Guid.NewGuid();
        var refs = new[] { blobId.ToString() };
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            Status = MenuSessionStatus.Review,
            ImageRefs = refs.ToList(),
            ParsedItems = ["Existing"]
        };
        var evt = new MenuParsingRequestedEvent(sessionId, userId, refs);

        _menuSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        await _sut.ProcessAsync(evt);

        _imageBlobRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _menuIntelligenceClient.Verify(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()), Times.Never);
        _menuSessionRepo.Verify(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenMatchingSessionHasConfirmedItems_ReusesThemWithoutCallingAi()
    {
        var sessionId = Guid.NewGuid();
        var matchingSessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var blobId = Guid.NewGuid();
        var imageHash = "ABC123";
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            ImageRefs = [blobId.ToString()],
            Status = MenuSessionStatus.Uploaded
        };
        var matchingSession = new MenuSession
        {
            Id = matchingSessionId,
            UserId = Guid.NewGuid(),
            Status = MenuSessionStatus.Confirmed,
            ConfirmedItems = ["Kebab box", "Baklawa"],
            UpdatedAtUtc = DateTime.UtcNow
        };
        var evt = new MenuParsingRequestedEvent(sessionId, userId, [blobId.ToString()]);
        var blob = new Domain.Entities.ImageBlob { Id = blobId, Data = [0x01, 0x02], ImageHash = imageHash };

        _menuSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _menuSessionRepo.Setup(r => r.GetByIdAsync(matchingSessionId, It.IsAny<CancellationToken>())).ReturnsAsync(matchingSession);
        _menuSessionRepo.Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>())).ReturnsAsync((MenuSession s, CancellationToken _) => s);
        _imageBlobRepo.Setup(r => r.GetByIdAsync(blobId, It.IsAny<CancellationToken>())).ReturnsAsync(blob);
        _imageBlobRepo
            .Setup(r => r.FindSessionIdsByExactImageHashesAsync(
                It.Is<IReadOnlyList<string>>(hashes => hashes.Count == 1 && hashes[0] == imageHash),
                sessionId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([matchingSessionId]);

        await _sut.ProcessAsync(evt);

        _menuIntelligenceClient.Verify(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()), Times.Never);
        _menuSessionRepo.Verify(r => r.UpdateAsync(It.Is<MenuSession>(s =>
            s.Status == MenuSessionStatus.Review &&
            s.ParsedItems.SequenceEqual(new[] { "Kebab box", "Baklawa" }) &&
            s.ConfirmedItems.SequenceEqual(new[] { "Kebab box", "Baklawa" })), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_WhenMatchingSessionHasNoConfirmedItems_FallsBackToAi()
    {
        var sessionId = Guid.NewGuid();
        var matchingSessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var blobId = Guid.NewGuid();
        var session = new MenuSession
        {
            Id = sessionId,
            UserId = userId,
            ImageRefs = [blobId.ToString()],
            Status = MenuSessionStatus.Uploaded
        };
        var matchingSession = new MenuSession
        {
            Id = matchingSessionId,
            UserId = Guid.NewGuid(),
            Status = MenuSessionStatus.Review,
            ConfirmedItems = []
        };
        var evt = new MenuParsingRequestedEvent(sessionId, userId, [blobId.ToString()]);
        var blob = new Domain.Entities.ImageBlob { Id = blobId, Data = [0x01, 0x02], ImageHash = "HASH-1" };

        _menuSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _menuSessionRepo.Setup(r => r.GetByIdAsync(matchingSessionId, It.IsAny<CancellationToken>())).ReturnsAsync(matchingSession);
        _menuSessionRepo.Setup(r => r.UpdateAsync(It.IsAny<MenuSession>(), It.IsAny<CancellationToken>())).ReturnsAsync((MenuSession s, CancellationToken _) => s);
        _imageBlobRepo.Setup(r => r.GetByIdAsync(blobId, It.IsAny<CancellationToken>())).ReturnsAsync(blob);
        _imageBlobRepo
            .Setup(r => r.FindSessionIdsByExactImageHashesAsync(It.IsAny<IReadOnlyList<string>>(), sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([matchingSessionId]);
        _menuIntelligenceClient
            .Setup(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Falafel box"]);

        await _sut.ProcessAsync(evt);

        _menuIntelligenceClient.Verify(h => h.ExtractDishNamesFromImagesAsync(It.IsAny<IReadOnlyList<byte[]>>(), It.IsAny<CancellationToken>()), Times.Once);
        _menuSessionRepo.Verify(r => r.UpdateAsync(It.Is<MenuSession>(s =>
            s.Status == MenuSessionStatus.Review &&
            s.ParsedItems.SequenceEqual(new[] { "Falafel box" }) &&
            s.ConfirmedItems.Count == 0), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
