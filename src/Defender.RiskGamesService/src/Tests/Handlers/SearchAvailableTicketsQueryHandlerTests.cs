using Defender.Common.Exceptions;
using Defender.RiskGamesService.Application.Common.Interfaces.Services.Lottery;
using Defender.RiskGamesService.Application.Modules.Lottery.Queries;
using Defender.RiskGamesService.Domain.Entities.Lottery.Draw;
using Defender.RiskGamesService.Domain.Entities.Lottery.UserTickets;

namespace Defender.RiskGamesService.Tests.Handlers;

public class SearchAvailableTicketsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenDrawInactive_ThrowsServiceException()
    {
        var ticketService = new Mock<IUserTicketManagementService>();
        var lotteryService = new Mock<ILotteryManagementService>();
        ticketService.Setup(x => x.GetUserTicketsByDrawNumberAsync(1)).ReturnsAsync([]);
        lotteryService.Setup(x => x.GetLotteryDrawByNumberAsync(1)).ReturnsAsync(new LotteryDraw
        {
            DrawNumber = 1,
            IsProcessing = false,
            IsProcessed = false,
            StartDate = DateTime.UtcNow.AddDays(-2),
            EndDate = DateTime.UtcNow.AddDays(-1)
        });
        var sut = new SearchAvailableTicketsQueryHandler(ticketService.Object, lotteryService.Object);

        await Assert.ThrowsAsync<ServiceException>(() => sut.Handle(new SearchAvailableTicketsQuery
        {
            DrawNumber = 1,
            AmountOfTickets = 2,
            TargetTicket = 0
        }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenRandomRequested_ReturnsSortedRandomSubset()
    {
        var ticketService = new Mock<IUserTicketManagementService>();
        var lotteryService = new Mock<ILotteryManagementService>();
        ticketService.Setup(x => x.GetUserTicketsByDrawNumberAsync(2))
            .ReturnsAsync([new UserTicket { TicketNumber = 2 }, new UserTicket { TicketNumber = 3 }]);
        lotteryService.Setup(x => x.GetLotteryDrawByNumberAsync(2)).ReturnsAsync(new LotteryDraw
        {
            DrawNumber = 2,
            MinTicketNumber = 1,
            MaxTicketNumber = 10,
            StartDate = DateTime.UtcNow.AddMinutes(-5),
            EndDate = DateTime.UtcNow.AddMinutes(30)
        });
        var sut = new SearchAvailableTicketsQueryHandler(ticketService.Object, lotteryService.Object);

        var result = await sut.Handle(new SearchAvailableTicketsQuery
        {
            DrawNumber = 2,
            AmountOfTickets = 3,
            TargetTicket = 0
        }, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(2, result);
        Assert.DoesNotContain(3, result);
        Assert.True(result.SequenceEqual(result.OrderBy(x => x)));
    }

    [Fact]
    public async Task Handle_WhenTargetProvided_ReturnsClosestNumbers()
    {
        var ticketService = new Mock<IUserTicketManagementService>();
        var lotteryService = new Mock<ILotteryManagementService>();
        ticketService.Setup(x => x.GetUserTicketsByDrawNumberAsync(3)).ReturnsAsync([]);
        lotteryService.Setup(x => x.GetLotteryDrawByNumberAsync(3)).ReturnsAsync(new LotteryDraw
        {
            DrawNumber = 3,
            MinTicketNumber = 1,
            MaxTicketNumber = 10,
            StartDate = DateTime.UtcNow.AddMinutes(-5),
            EndDate = DateTime.UtcNow.AddMinutes(30)
        });
        var sut = new SearchAvailableTicketsQueryHandler(ticketService.Object, lotteryService.Object);

        var result = await sut.Handle(new SearchAvailableTicketsQuery
        {
            DrawNumber = 3,
            AmountOfTickets = 4,
            TargetTicket = 7
        }, CancellationToken.None);

        Assert.Equal(4, result.Count);
        Assert.Contains(7, result);
        Assert.True(result.SequenceEqual(result.OrderBy(x => x)));
    }
}
