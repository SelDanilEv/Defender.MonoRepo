using Defender.Common.Enums;
using Defender.Common.Interfaces;
using Defender.HealthCareService.Application.Common.Interfaces.Repositories;
using Defender.HealthCareService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers.V1;

namespace Defender.HealthCareService.Tests.Controllers;

public class HealthChartSharesControllerTests
{
    [Fact]
    public async Task GetPublicShare_WhenShareExists_ReturnsStoredDateRange()
    {
        var from = DateTimeOffset.Parse("2026-06-20T08:00:00Z");
        var to = DateTimeOffset.Parse("2026-06-21T08:00:00Z");
        var token = "share-token";
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
                CreatedAtUtc = DateTimeOffset.Parse("2026-06-21T09:00:00Z"),
            });
        var controller = CreateController(healthEventRepository, shareRepository);

        var result = await controller.GetPublicShare(token);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HealthChartShareDto>(okResult.Value);
        Assert.Equal(from, dto.From);
        Assert.Equal(to, dto.To);
    }

    private static HealthChartSharesController CreateController(
        Mock<IHealthEventRepository> healthEventRepository,
        Mock<IHealthChartShareRepository> shareRepository)
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
            shareRepository.Object);
    }
}
