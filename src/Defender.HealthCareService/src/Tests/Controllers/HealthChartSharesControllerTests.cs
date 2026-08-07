using Defender.Common.Enums;
using Defender.Common.Interfaces;
using Defender.HealthCareService.Application.Common.Interfaces.Repositories;
using Defender.HealthCareService.Domain.Entities;
using Defender.HealthCareService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers.V1;

namespace Defender.HealthCareService.Tests.Controllers;

public class HealthChartSharesControllerTests
{
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.Parse("2026-08-07T12:00:00Z");

    [Fact]
    public async Task GetPublicShare_WhenRangeModeIsAbsolute_PreservesExactDateRange()
    {
        var from = DateTimeOffset.Parse("2026-06-18T08:30:00Z");
        var to = DateTimeOffset.Parse("2026-06-19T17:00:00Z");
        var token = "absolute-token";
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var healthEventRepository = new Mock<IHealthEventRepository>();
        healthEventRepository
            .Setup(x => x.GetHealthEventsAsync(userId, from, to))
            .ReturnsAsync([]);
        var shareRepository = new Mock<IHealthChartShareRepository>();
        shareRepository
            .Setup(x => x.GetHealthChartShareByTokenAsync(token))
            .ReturnsAsync(new HealthChartShare
            {
                Token = token,
                UserId = userId,
                From = from,
                To = to,
                RangeMode = HealthChartShareRangeMode.Absolute,
            });
        var controller = CreateController(healthEventRepository, shareRepository);

        var result = await controller.GetPublicShare(token);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HealthChartShareDto>(okResult.Value);
        Assert.Equal(from, dto.From);
        Assert.Equal(to, dto.To);
        Assert.Equal(HealthChartShareRangeMode.Absolute, dto.RangeMode);
        healthEventRepository.Verify(x => x.GetHealthEventsAsync(userId, from, to), Times.Once);
    }

    [Fact]
    public async Task GetPublicShare_WhenRangeModeIsRolling_UsesCurrentRollingWindow()
    {
        var storedFrom = DateTimeOffset.Parse("2026-06-20T08:00:00Z");
        var storedTo = DateTimeOffset.Parse("2026-06-21T08:00:00Z");
        var expectedFrom = FixedNow - (storedTo - storedFrom);
        var token = "rolling-token";
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var healthEventRepository = new Mock<IHealthEventRepository>();
        DateTimeOffset? requestedFrom = null;
        DateTimeOffset? requestedTo = null;
        healthEventRepository
            .Setup(x => x.GetHealthEventsAsync(userId, It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>()))
            .Callback<Guid, DateTimeOffset?, DateTimeOffset?>((_, from, to) =>
            {
                requestedFrom = from;
                requestedTo = to;
            })
            .ReturnsAsync([]);
        var shareRepository = new Mock<IHealthChartShareRepository>();
        shareRepository
            .Setup(x => x.GetHealthChartShareByTokenAsync(token))
            .ReturnsAsync(new HealthChartShare
            {
                Token = token,
                UserId = userId,
                From = storedFrom,
                To = storedTo,
                RangeMode = HealthChartShareRangeMode.Rolling,
            });
        var controller = CreateController(healthEventRepository, shareRepository);

        var result = await controller.GetPublicShare(token);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HealthChartShareDto>(okResult.Value);
        Assert.Equal(expectedFrom, requestedFrom);
        Assert.Equal(FixedNow, requestedTo);
        Assert.Equal(expectedFrom, dto.From);
        Assert.Equal(FixedNow, dto.To);
        Assert.Equal(HealthChartShareRangeMode.Rolling, dto.RangeMode);
    }

    [Fact]
    public async Task GetPublicShare_WhenRangeModeIsAll_DoesNotApplyBounds()
    {
        var token = "all-token";
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var healthEventRepository = new Mock<IHealthEventRepository>();
        healthEventRepository
            .Setup(x => x.GetHealthEventsAsync(userId, null, null))
            .ReturnsAsync([]);
        var shareRepository = new Mock<IHealthChartShareRepository>();
        shareRepository
            .Setup(x => x.GetHealthChartShareByTokenAsync(token))
            .ReturnsAsync(new HealthChartShare
            {
                Token = token,
                UserId = userId,
                RangeMode = HealthChartShareRangeMode.All,
            });
        var controller = CreateController(healthEventRepository, shareRepository);

        var result = await controller.GetPublicShare(token);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HealthChartShareDto>(okResult.Value);
        Assert.Null(dto.From);
        Assert.Null(dto.To);
        Assert.Equal(HealthChartShareRangeMode.All, dto.RangeMode);
    }

    [Fact]
    public async Task GetPublicShare_WhenLegacyShareHasOneBound_PreservesStoredBoundsAsAbsolute()
    {
        var from = DateTimeOffset.Parse("2026-06-18T08:30:00Z");
        var token = "legacy-one-sided-token";
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var healthEventRepository = new Mock<IHealthEventRepository>();
        healthEventRepository
            .Setup(x => x.GetHealthEventsAsync(userId, from, null))
            .ReturnsAsync([]);
        var shareRepository = new Mock<IHealthChartShareRepository>();
        shareRepository
            .Setup(x => x.GetHealthChartShareByTokenAsync(token))
            .ReturnsAsync(new HealthChartShare
            {
                Token = token,
                UserId = userId,
                From = from,
                RangeMode = null,
            });
        var controller = CreateController(healthEventRepository, shareRepository);

        var result = await controller.GetPublicShare(token);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HealthChartShareDto>(okResult.Value);
        Assert.Equal(from, dto.From);
        Assert.Null(dto.To);
        Assert.Equal(HealthChartShareRangeMode.Absolute, dto.RangeMode);
    }

    [Fact]
    public async Task CreateShare_WhenAbsoluteRangeIsValid_PersistsModeAndUsesFixedTimestamp()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var from = DateTimeOffset.Parse("2026-06-18T08:30:00Z");
        var to = DateTimeOffset.Parse("2026-06-19T17:00:00Z");
        HealthChartShare? addedShare = null;
        var healthEventRepository = new Mock<IHealthEventRepository>();
        healthEventRepository
            .Setup(x => x.GetHealthEventsAsync(userId, from, to))
            .ReturnsAsync([]);
        var shareRepository = new Mock<IHealthChartShareRepository>();
        shareRepository
            .Setup(x => x.GetHealthChartShareByUserIdAsync(userId))
            .ReturnsAsync((HealthChartShare?)null);
        shareRepository
            .Setup(x => x.AddHealthChartShareAsync(It.IsAny<HealthChartShare>()))
            .Callback<HealthChartShare>(share => addedShare = share)
            .ReturnsAsync((HealthChartShare share) => share);
        var controller = CreateController(healthEventRepository, shareRepository);

        var result = await controller.CreateShare(
            new HealthChartShareRequest(from, to, HealthChartShareRangeMode.Absolute));

        var createdResult = Assert.IsType<CreatedResult>(result.Result);
        var dto = Assert.IsType<HealthChartShareDto>(createdResult.Value);
        Assert.NotNull(addedShare);
        Assert.Equal(HealthChartShareRangeMode.Absolute, addedShare.RangeMode);
        Assert.Equal(FixedNow, addedShare.CreatedAtUtc);
        Assert.Equal(HealthChartShareRangeMode.Absolute, dto.RangeMode);
    }

    [Theory]
    [InlineData(HealthChartShareRangeMode.Absolute, true, false)]
    [InlineData(HealthChartShareRangeMode.Rolling, false, true)]
    [InlineData(HealthChartShareRangeMode.All, false, false)]
    public async Task CreateShare_WhenRangeModePayloadIsInvalid_ReturnsBadRequest(
        HealthChartShareRangeMode mode,
        bool missingFrom,
        bool reversed)
    {
        var from = missingFrom
            ? (DateTimeOffset?)null
            : DateTimeOffset.Parse("2026-06-19T08:00:00Z");
        var to = mode == HealthChartShareRangeMode.All
            ? DateTimeOffset.Parse("2026-06-20T08:00:00Z")
            : DateTimeOffset.Parse(reversed ? "2026-06-18T08:00:00Z" : "2026-06-20T08:00:00Z");
        var controller = CreateController(new Mock<IHealthEventRepository>(), new Mock<IHealthChartShareRepository>());

        var result = await controller.CreateShare(new HealthChartShareRequest(from, to, mode));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPublicShare_WhenShareExists_ReturnsCurrentRollingDateRange()
    {
        var from = DateTimeOffset.Parse("2026-06-20T08:00:00Z");
        var to = DateTimeOffset.Parse("2026-06-21T08:00:00Z");
        var token = "share-token";
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var healthEventRepository = new Mock<IHealthEventRepository>();
        DateTimeOffset? requestedFrom = null;
        DateTimeOffset? requestedTo = null;
        healthEventRepository
            .Setup(x => x.GetHealthEventsAsync(userId, It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>()))
            .Callback<Guid, DateTimeOffset?, DateTimeOffset?>((_, rangeFrom, rangeTo) =>
            {
                requestedFrom = rangeFrom;
                requestedTo = rangeTo;
            })
            .ReturnsAsync([]);
        var shareRepository = new Mock<IHealthChartShareRepository>();
        shareRepository
            .Setup(x => x.GetHealthChartShareByTokenAsync(token))
            .ReturnsAsync(new HealthChartShare
            {
                Token = token,
                UserId = userId,
                From = from,
                To = to,
                CreatedAtUtc = DateTimeOffset.Parse("2026-06-21T09:00:00Z"),
            });
        var controller = CreateController(healthEventRepository, shareRepository);

        var result = await controller.GetPublicShare(token);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HealthChartShareDto>(okResult.Value);
        Assert.NotNull(requestedFrom);
        Assert.NotNull(requestedTo);
        Assert.NotEqual(from, requestedFrom);
        Assert.NotEqual(to, requestedTo);
        var requestedRangeSeconds = (requestedTo.Value - requestedFrom.Value).TotalSeconds;
        var expectedRangeSeconds = TimeSpan.FromDays(1).TotalSeconds;
        Assert.InRange(requestedRangeSeconds, expectedRangeSeconds - 1, expectedRangeSeconds + 1);
        Assert.Equal(requestedFrom, dto.From);
        Assert.Equal(requestedTo, dto.To);
        Assert.True(dto.IsEnabled);
    }

    [Fact]
    public async Task GetPublicShare_WhenEventsAreReturnedOldestFirst_ReturnsMostRecentEventsFirst()
    {
        var token = "share-token";
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var oldEvent = new HealthEvent
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            UserId = userId,
            Type = HealthEventType.Temperature,
            StartedAt = DateTimeOffset.Parse("2026-06-21T08:00:00Z"),
        };
        var newEvent = new HealthEvent
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UserId = userId,
            Type = HealthEventType.Medication,
            StartedAt = DateTimeOffset.Parse("2026-06-21T10:00:00Z"),
        };
        var healthEventRepository = new Mock<IHealthEventRepository>();
        healthEventRepository
            .Setup(x => x.GetHealthEventsAsync(userId, null, null))
            .ReturnsAsync([oldEvent, newEvent]);
        var shareRepository = new Mock<IHealthChartShareRepository>();
        shareRepository
            .Setup(x => x.GetHealthChartShareByTokenAsync(token))
            .ReturnsAsync(new HealthChartShare
            {
                Token = token,
                UserId = userId,
                IsEnabled = true,
                CreatedAtUtc = DateTimeOffset.Parse("2026-06-21T09:00:00Z"),
            });
        var controller = CreateController(healthEventRepository, shareRepository);

        var result = await controller.GetPublicShare(token);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HealthChartShareDto>(okResult.Value);
        Assert.Equal([newEvent.Id, oldEvent.Id], dto.Events.Select(x => x.Id));
    }

    [Fact]
    public async Task GetPublicShare_WhenShareIsDisabled_ReturnsNotFound()
    {
        var token = "share-token";
        var shareRepository = new Mock<IHealthChartShareRepository>();
        shareRepository
            .Setup(x => x.GetHealthChartShareByTokenAsync(token))
            .ReturnsAsync(new HealthChartShare
            {
                Token = token,
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                IsEnabled = false,
            });
        var controller = CreateController(new Mock<IHealthEventRepository>(), shareRepository);

        var result = await controller.GetPublicShare(token);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateShare_WhenUserAlreadyHasShare_ReusesTokenAndEnablesSharing()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var from = DateTimeOffset.Parse("2026-06-20T08:00:00Z");
        var to = DateTimeOffset.Parse("2026-06-21T08:00:00Z");
        var existingShare = new HealthChartShare
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Token = "stable-token",
            UserId = userId,
            IsEnabled = false,
            CreatedAtUtc = DateTimeOffset.Parse("2026-06-19T08:00:00Z"),
        };
        HealthChartShare? updatedShare = null;
        var healthEventRepository = new Mock<IHealthEventRepository>();
        healthEventRepository
            .Setup(x => x.GetHealthEventsAsync(userId, from, to))
            .ReturnsAsync([]);
        var shareRepository = new Mock<IHealthChartShareRepository>();
        shareRepository
            .Setup(x => x.GetHealthChartShareByUserIdAsync(userId))
            .ReturnsAsync(existingShare);
        shareRepository
            .Setup(x => x.UpdateHealthChartShareAsync(It.IsAny<HealthChartShare>()))
            .Callback<HealthChartShare>(share => updatedShare = share)
            .ReturnsAsync((HealthChartShare share) => share);
        var controller = CreateController(healthEventRepository, shareRepository);

        var result = await controller.CreateShare(new HealthChartShareRequest(from, to));

        var createdResult = Assert.IsType<CreatedResult>(result.Result);
        var dto = Assert.IsType<HealthChartShareDto>(createdResult.Value);
        Assert.Equal("stable-token", dto.Token);
        Assert.True(dto.IsEnabled);
        Assert.Equal(from, dto.From);
        Assert.Equal(to, dto.To);
        Assert.NotNull(updatedShare);
        Assert.Equal(existingShare.Id, updatedShare.Id);
        shareRepository.Verify(x => x.AddHealthChartShareAsync(It.IsAny<HealthChartShare>()), Times.Never);
        shareRepository.Verify(x => x.DisableOtherHealthChartSharesAsync(userId, existingShare.Id), Times.Once);
    }

    [Fact]
    public async Task UpdateShareStatus_WhenShareExists_UpdatesStatusWithoutChangingToken()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var existingShare = new HealthChartShare
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Token = "stable-token",
            UserId = userId,
            IsEnabled = true,
            CreatedAtUtc = DateTimeOffset.Parse("2026-06-19T08:00:00Z"),
        };
        HealthChartShare? updatedShare = null;
        var shareRepository = new Mock<IHealthChartShareRepository>();
        shareRepository
            .Setup(x => x.GetHealthChartShareByUserIdAsync(userId))
            .ReturnsAsync(existingShare);
        shareRepository
            .Setup(x => x.UpdateHealthChartShareAsync(It.IsAny<HealthChartShare>()))
            .Callback<HealthChartShare>(share => updatedShare = share)
            .ReturnsAsync((HealthChartShare share) => share);
        var controller = CreateController(new Mock<IHealthEventRepository>(), shareRepository);

        var result = await controller.UpdateShareStatus(new HealthChartShareStatusRequest(false));

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HealthChartShareDto>(okResult.Value);
        Assert.Equal("stable-token", dto.Token);
        Assert.False(dto.IsEnabled);
        Assert.NotNull(updatedShare);
        Assert.False(updatedShare.IsEnabled);
        shareRepository.Verify(x => x.DisableOtherHealthChartSharesAsync(userId, existingShare.Id), Times.Once);
    }

    [Fact]
    public async Task GetCurrentShare_WhenShareExists_ReturnsStableTokenAndStatus()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var existingShare = new HealthChartShare
        {
            Token = "stable-token",
            UserId = userId,
            IsEnabled = false,
            CreatedAtUtc = DateTimeOffset.Parse("2026-06-19T08:00:00Z"),
        };
        var shareRepository = new Mock<IHealthChartShareRepository>();
        shareRepository
            .Setup(x => x.GetHealthChartShareByUserIdAsync(userId))
            .ReturnsAsync(existingShare);
        var controller = CreateController(new Mock<IHealthEventRepository>(), shareRepository);

        var result = await controller.GetCurrentShare();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HealthChartShareDto>(okResult.Value);
        Assert.Equal("stable-token", dto.Token);
        Assert.False(dto.IsEnabled);
    }

    private static HealthChartSharesController CreateController(
        Mock<IHealthEventRepository> healthEventRepository,
        Mock<IHealthChartShareRepository> shareRepository,
        DateTimeOffset? now = null)
    {
        var currentAccountAccessor = new Mock<ICurrentAccountAccessor>();
        currentAccountAccessor
            .Setup(x => x.GetAccountId())
            .Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        currentAccountAccessor
            .Setup(x => x.GetRoles())
            .Returns([]);
        currentAccountAccessor
            .Setup(x => x.GetHighestRole())
            .Returns(Role.User);

        return new HealthChartSharesController(
            currentAccountAccessor.Object,
            healthEventRepository.Object,
            shareRepository.Object,
            new FixedTimeProvider(now ?? FixedNow));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
