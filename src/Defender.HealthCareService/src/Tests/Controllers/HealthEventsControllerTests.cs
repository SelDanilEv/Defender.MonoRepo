using Defender.Common.Enums;
using Defender.Common.Interfaces;
using Defender.HealthCareService.Application.Common.Interfaces.Repositories;
using Defender.HealthCareService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers.V1;

namespace Defender.HealthCareService.Tests.Controllers;

public class HealthEventsControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateEvent_WhenWellbeingScoreIsInvalid_ReturnsBadRequest(int? wellbeingScore)
    {
        var repository = new Mock<IHealthEventRepository>();
        var controller = CreateController(repository);

        var result = await controller.CreateEvent(new HealthEvent
        {
            Type = HealthEventType.Wellbeing,
            StartedAt = DateTimeOffset.UtcNow,
            WellbeingScore = wellbeingScore,
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        repository.Verify(
            x => x.AddHealthEventAsync(It.IsAny<HealthEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEvent_WhenNonWellbeingHasWellbeingScore_ClearsScoreBeforeSaving()
    {
        HealthEvent? savedEvent = null;
        var repository = new Mock<IHealthEventRepository>();
        repository
            .Setup(x => x.AddHealthEventAsync(It.IsAny<HealthEvent>()))
            .Callback<HealthEvent>(healthEvent => savedEvent = healthEvent)
            .ReturnsAsync((HealthEvent healthEvent) => healthEvent);
        var controller = CreateController(repository);

        await controller.CreateEvent(new HealthEvent
        {
            Type = HealthEventType.Temperature,
            StartedAt = DateTimeOffset.UtcNow,
            TemperatureCelsius = 37.2m,
            WellbeingScore = 5,
        });

        Assert.NotNull(savedEvent);
        Assert.Null(savedEvent.WellbeingScore);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(36.3)]
    [InlineData(40.6)]
    public async Task CreateEvent_WhenTemperatureIsInvalid_ReturnsBadRequest(double? temperatureCelsius)
    {
        var repository = new Mock<IHealthEventRepository>();
        var controller = CreateController(repository);

        var result = await controller.CreateEvent(new HealthEvent
        {
            Type = HealthEventType.Temperature,
            StartedAt = DateTimeOffset.UtcNow,
            TemperatureCelsius = temperatureCelsius == null ? null : Convert.ToDecimal(temperatureCelsius),
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        repository.Verify(
            x => x.AddHealthEventAsync(It.IsAny<HealthEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEvent_WhenNonTemperatureHasTemperature_ClearsTemperatureBeforeSaving()
    {
        HealthEvent? savedEvent = null;
        var repository = new Mock<IHealthEventRepository>();
        repository
            .Setup(x => x.AddHealthEventAsync(It.IsAny<HealthEvent>()))
            .Callback<HealthEvent>(healthEvent => savedEvent = healthEvent)
            .ReturnsAsync((HealthEvent healthEvent) => healthEvent);
        var controller = CreateController(repository);

        await controller.CreateEvent(new HealthEvent
        {
            Type = HealthEventType.Wellbeing,
            StartedAt = DateTimeOffset.UtcNow,
            TemperatureCelsius = 37.2m,
            WellbeingScore = 5,
        });

        Assert.NotNull(savedEvent);
        Assert.Null(savedEvent.TemperatureCelsius);
    }

    [Fact]
    public async Task GetMedicationOptions_WhenMedicationEventsExist_ReturnsDistinctSortedOptions()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var repository = new Mock<IHealthEventRepository>();
        repository
            .Setup(x => x.GetHealthEventsAsync(userId, null, null))
            .ReturnsAsync(
            [
                new HealthEvent
                {
                    Type = HealthEventType.Temperature,
                    MedicationName = "Ignored",
                    MedicationAmount = 100m,
                    MedicationUnit = "Ignored",
                },
                new HealthEvent
                {
                    Type = HealthEventType.Medication,
                    MedicationName = "Ibuprofen",
                    MedicationAmount = 2m,
                    MedicationUnit = "tablet",
                },
                new HealthEvent
                {
                    Type = HealthEventType.Medication,
                    MedicationName = " ibuprofen ",
                    MedicationAmount = 2m,
                    MedicationUnit = " Tablet ",
                },
                new HealthEvent
                {
                    Type = HealthEventType.Medication,
                    MedicationName = "Paracetamol",
                    MedicationAmount = 0.5m,
                    MedicationUnit = "mg",
                },
            ]);
        var controller = CreateController(repository);

        var result = await controller.GetMedicationOptions();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var options = Assert.IsType<MedicationOptionsResponse>(okResult.Value);
        Assert.Equal(["Ibuprofen", "Paracetamol"], options.Names);
        Assert.Equal(["0.5", "2"], options.Amounts);
        Assert.Equal(["mg", "tablet"], options.Units);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateEvent_WhenAnalysisNameIsMissing_ReturnsBadRequest(string? analysisName)
    {
        var repository = new Mock<IHealthEventRepository>();
        var controller = CreateController(repository);

        var result = await controller.CreateEvent(new HealthEvent
        {
            Type = HealthEventType.Analysis,
            StartedAt = DateTimeOffset.UtcNow,
            AnalysisName = analysisName,
            AnalysisStatus = AnalysisStatus.Bad,
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        repository.Verify(
            x => x.AddHealthEventAsync(It.IsAny<HealthEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEvent_WhenAnalysisStatusIsMissing_ReturnsBadRequest()
    {
        var repository = new Mock<IHealthEventRepository>();
        var controller = CreateController(repository);

        var result = await controller.CreateEvent(new HealthEvent
        {
            Type = HealthEventType.Analysis,
            StartedAt = DateTimeOffset.UtcNow,
            AnalysisName = "CRP",
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        repository.Verify(
            x => x.AddHealthEventAsync(It.IsAny<HealthEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEvent_WhenNonAnalysisHasAnalysisFields_ClearsThemBeforeSaving()
    {
        HealthEvent? savedEvent = null;
        var repository = new Mock<IHealthEventRepository>();
        repository
            .Setup(x => x.AddHealthEventAsync(It.IsAny<HealthEvent>()))
            .Callback<HealthEvent>(healthEvent => savedEvent = healthEvent)
            .ReturnsAsync((HealthEvent healthEvent) => healthEvent);
        var controller = CreateController(repository);

        await controller.CreateEvent(new HealthEvent
        {
            Type = HealthEventType.Temperature,
            StartedAt = DateTimeOffset.UtcNow,
            TemperatureCelsius = 37.2m,
            AnalysisName = "CRP",
            AnalysisStatus = AnalysisStatus.HasDeviations,
        });

        Assert.NotNull(savedEvent);
        Assert.Null(savedEvent.AnalysisName);
        Assert.Null(savedEvent.AnalysisStatus);
    }

    private static HealthEventsController CreateController(Mock<IHealthEventRepository> repository)
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

        return new HealthEventsController(currentAccountAccessor.Object, repository.Object);
    }
}
