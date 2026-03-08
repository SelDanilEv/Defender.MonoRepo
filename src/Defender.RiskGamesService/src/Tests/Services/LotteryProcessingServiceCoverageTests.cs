using Defender.Common.DB.Model;
using Defender.Kafka.Default;
using Defender.RiskGamesService.Application.Common.Interfaces.Repositories.Lottery;
using Defender.RiskGamesService.Application.Common.Interfaces.Services.Lottery;
using Defender.RiskGamesService.Application.Services.Lottery;
using Defender.RiskGamesService.Common.Kafka;
using Defender.RiskGamesService.Domain.Entities.Lottery.Draw;
using Defender.RiskGamesService.Domain.Entities.Lottery.TicketsSettings;
using Defender.RiskGamesService.Domain.Enums;

namespace Defender.RiskGamesService.Tests.Services;

public class LotteryProcessingServiceCoverageTests
{
    [Fact]
    public async Task QueueLotteriesForProcessing_WhenIdsExist_ProducesKafkaMessages()
    {
        var producer = new Mock<IDefaultKafkaProducer<Guid>>();
        var repo = new Mock<ILotteryDrawRepository>();
        var tickets = new Mock<IUserTicketManagementService>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        repo.Setup(x => x.GetLotteryDrawsToProcessAsync(It.IsAny<CancellationToken>())).ReturnsAsync([id1, id2]);
        producer.Setup(x => x.ProduceAsync(KafkaTopic.LotteryToProcess.GetName(), id1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        producer.Setup(x => x.ProduceAsync(KafkaTopic.LotteryToProcess.GetName(), id2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = new LotteryProcessingService(producer.Object, repo.Object, tickets.Object);

        await sut.QueueLotteriesForProcessing();

        producer.VerifyAll();
    }

    [Fact]
    public async Task HandleLotteryDraw_WhenNotProcessing_ReturnsWithoutUpdates()
    {
        var producer = new Mock<IDefaultKafkaProducer<Guid>>();
        var repo = new Mock<ILotteryDrawRepository>();
        var tickets = new Mock<IUserTicketManagementService>();
        var drawId = Guid.NewGuid();
        repo.Setup(x => x.GetLotteryDrawAsync(drawId)).ReturnsAsync(new LotteryDraw
        {
            Id = drawId,
            IsProcessing = false,
            IsProcessed = false,
            Winnings = []
        });
        var sut = new LotteryProcessingService(producer.Object, repo.Object, tickets.Object);

        await sut.HandleLotteryDraw(drawId);

        repo.Verify(x => x.UpdateLotteryDrawAsync(It.IsAny<UpdateModelRequest<LotteryDraw>>()), Times.Never);
        tickets.Verify(x => x.CheckWinningsAsync(It.IsAny<LotteryDraw>()), Times.Never);
    }

    [Fact]
    public async Task HandleLotteryDraw_WhenProcessingAndNoWinnings_FillsWinningsAndUpdates()
    {
        var producer = new Mock<IDefaultKafkaProducer<Guid>>();
        var repo = new Mock<ILotteryDrawRepository>();
        var tickets = new Mock<IUserTicketManagementService>();
        var drawId = Guid.NewGuid();
        var draw = new LotteryDraw
        {
            Id = drawId,
            DrawNumber = 5,
            MinTicketNumber = 1,
            MaxTicketNumber = 20,
            IsProcessing = true,
            IsProcessed = false,
            PrizeSetup = new TicketsPrizeSetup
            {
                Prizes =
                [
                    new TicketPrize { TicketsAmount = 2, Coefficient = 200 },
                    new TicketPrize { TicketsAmount = 3, Coefficient = 150 }
                ]
            },
            Winnings =
            [
                new Winning { Coefficient = 200, Tickets = [] },
                new Winning { Coefficient = 150, Tickets = [] }
            ]
        };
        repo.Setup(x => x.GetLotteryDrawAsync(drawId)).ReturnsAsync(draw);
        repo.Setup(x => x.UpdateLotteryDrawAsync(It.IsAny<UpdateModelRequest<LotteryDraw>>())).ReturnsAsync(draw);
        tickets.Setup(x => x.CheckWinningsAsync(draw)).Returns(Task.CompletedTask);
        var sut = new LotteryProcessingService(producer.Object, repo.Object, tickets.Object);

        await sut.HandleLotteryDraw(drawId);

        tickets.Verify(x => x.CheckWinningsAsync(draw), Times.Once);
        repo.Verify(x => x.UpdateLotteryDrawAsync(It.IsAny<UpdateModelRequest<LotteryDraw>>()), Times.Once);
        Assert.All(draw.Winnings, w => Assert.NotEmpty(w.Tickets));
    }
}
