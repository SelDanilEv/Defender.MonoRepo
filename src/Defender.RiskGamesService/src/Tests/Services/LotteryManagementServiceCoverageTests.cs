using Defender.Common.DB.Pagination;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.RiskGamesService.Application.Common.Interfaces.Repositories.Lottery;
using Defender.RiskGamesService.Application.Models.Lottery;
using Defender.RiskGamesService.Application.Services.Lottery;
using Defender.RiskGamesService.Domain.Entities.Lottery;
using Defender.RiskGamesService.Domain.Entities.Lottery.Draw;
using Defender.RiskGamesService.Domain.Entities.Lottery.Enums;
using Defender.RiskGamesService.Domain.Entities.Lottery.TicketsSettings;
using Defender.RiskGamesService.Domain.Enums;

namespace Defender.RiskGamesService.Tests.Services;

public class LotteryManagementServiceCoverageTests
{
    private readonly Mock<ILotteryRepository> _lotteryRepository = new();
    private readonly Mock<ILotteryDrawRepository> _drawRepository = new();

    [Fact]
    public async Task CreateLotteryAsync_WhenRequestInvalid_ThrowsServiceException()
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<ServiceException>(() => sut.CreateLotteryAsync(new CreateLotteryRequest()));

        Assert.Contains(ErrorCode.VL_InvalidRequest.ToString(), ex.Message);
    }

    [Fact]
    public async Task CreateLotteryAsync_WhenRequestValid_CreatesLottery()
    {
        var request = CreateValidRequest();
        _lotteryRepository
            .Setup(x => x.CreateNewLotteryAsync(It.IsAny<LotteryModel>()))
            .ReturnsAsync((LotteryModel model) => model);
        var sut = CreateSut();

        var result = await sut.CreateLotteryAsync(request);

        Assert.Equal(request.Name, result.Name);
        Assert.NotNull(result.Schedule);
        Assert.NotNull(result.TicketsSetup);
        _lotteryRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateLotteryAsync_WhenRequestNull_ThrowsServiceException()
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<ServiceException>(() => sut.UpdateLotteryAsync(null!));

        Assert.Contains(ErrorCode.VL_InvalidRequest.ToString(), ex.Message);
    }

    [Fact]
    public async Task UpdateLotteryAsync_WhenTicketDataChanges_UpdatesIncomeAndReturnsResult()
    {
        var request = new UpdateLotteryRequest
        {
            Id = Guid.NewGuid(),
            TicketsAmount = 100,
            Prizes =
            [
                new TicketPrize { TicketsAmount = 1, Coefficient = 100 },
                new TicketPrize { TicketsAmount = 2, Coefficient = 200 }
            ]
        };
        _lotteryRepository
            .Setup(x => x.UpdateLotteryAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<LotteryModel>>()))
            .ReturnsAsync(new LotteryModel { Id = request.Id });
        var sut = CreateSut();

        var result = await sut.UpdateLotteryAsync(request);

        Assert.Equal(request.Id, result.Id);
        _lotteryRepository.VerifyAll();
    }

    [Fact]
    public async Task ActivateDeactivateDelete_WhenCalled_InvokeRepository()
    {
        var id = Guid.NewGuid();
        _lotteryRepository
            .Setup(x => x.UpdateLotteryAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<LotteryModel>>()))
            .ReturnsAsync(new LotteryModel { Id = id });
        _lotteryRepository
            .Setup(x => x.DeleteLotteryAsync(id))
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.ActivateLotteryAsync(id);
        await sut.DeactivateLotteryAsync(id);
        await sut.DeleteLotteryAsync(id);

        _lotteryRepository.Verify(x => x.UpdateLotteryAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<LotteryModel>>()), Times.Exactly(2));
        _lotteryRepository.Verify(x => x.DeleteLotteryAsync(id), Times.Once);
    }

    [Fact]
    public async Task ScheduleDraws_WhenLotteryEligible_CreatesDraw()
    {
        var lottery = new LotteryModel
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            PublicNames = new Dictionary<string, string> { ["en"] = "risk" },
            Schedule = LotterySchedule.Create(LotteryScheduleType.Daily, 1, DateTime.UtcNow.AddDays(-2)),
            TicketsSetup = TicketsSetup.Create(
                ticketsAmount: 10,
                startTicketNumber: 1,
                allowedValues: [100],
                isCustomValueAllowed: false,
                minValue: 100,
                maxValue: 100,
                allowedCurrencies: [Currency.USD],
                prizes: [new TicketPrize { TicketsAmount = 1, Coefficient = 200 }])
        };

        _lotteryRepository
            .Setup(x => x.GetAllLotteriesToScheduleAsync())
            .ReturnsAsync([lottery]);
        _lotteryRepository
            .Setup(x => x.UpdateLotteryAsync(
                It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<LotteryModel>>(),
                It.IsAny<Defender.Common.DB.Model.FindModelRequest<LotteryModel>>()))
            .ReturnsAsync(lottery);
        _drawRepository
            .Setup(x => x.CreateLotteryDrawAsync(It.IsAny<LotteryDraw>()))
            .ReturnsAsync((LotteryDraw d) => d);
        var sut = CreateSut();

        await sut.ScheduleDraws();

        _drawRepository.Verify(x => x.CreateLotteryDrawAsync(It.IsAny<LotteryDraw>()), Times.Once);
    }

    [Fact]
    public async Task GetMethods_WhenCalled_DelegateToRepositories()
    {
        var lotteryId = Guid.NewGuid();
        _lotteryRepository.Setup(x => x.GetLotteryModelByIdAsync(lotteryId)).ReturnsAsync(new LotteryModel { Id = lotteryId });
        _lotteryRepository.Setup(x => x.GetLotteriesAsync(It.IsAny<PaginationRequest>(), "q"))
            .ReturnsAsync(new PagedResult<LotteryModel> { Items = [] });
        _drawRepository.Setup(x => x.GetLotteryDrawAsync(123L)).ReturnsAsync(new LotteryDraw { DrawNumber = 123 });
        _drawRepository.Setup(x => x.GetActiveLotteryDrawsAsync(It.IsAny<PaginationRequest>()))
            .ReturnsAsync(new PagedResult<LotteryDraw> { Items = [] });
        var sut = CreateSut();

        var lottery = await sut.GetLotteryAsync(lotteryId);
        var lotteries = await sut.GetLotteriesAsync(new PaginationRequest(), "q");
        var draw = await sut.GetLotteryDrawByNumberAsync(123L);
        var draws = await sut.GetActiveDrawAsync(new PaginationRequest());

        Assert.Equal(lotteryId, lottery.Id);
        Assert.NotNull(lotteries);
        Assert.Equal(123L, draw.DrawNumber);
        Assert.NotNull(draws);
    }

    private LotteryManagementService CreateSut() => new(_lotteryRepository.Object, _drawRepository.Object);

    private static CreateLotteryRequest CreateValidRequest() => new()
    {
        Name = "L1",
        PublicNames = new Dictionary<string, string> { ["en"] = "L1" },
        ScheduleType = LotteryScheduleType.Daily,
        ScheduleCustomHours = 1,
        DurationType = LotteryScheduleType.Daily,
        DurationCustomHours = 1,
        StartDate = DateTime.UtcNow.AddDays(-1),
        FirstTicketNumber = 1,
        TicketsAmount = 100,
        AllowedValues = [100],
        IsCustomValueAllowed = false,
        MinBet = 100,
        MaxBet = 100,
        AllowedCurrencies = [Currency.USD],
        Prizes = [new TicketPrize { TicketsAmount = 1, Coefficient = 200 }]
    };
}
