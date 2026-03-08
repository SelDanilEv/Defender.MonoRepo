using Defender.Common.DB.SharedStorage.Enums;
using Defender.Common.Exceptions;
using Defender.Common.Interfaces;
using Defender.RiskGamesService.Application.Common.Interfaces.Repositories.Lottery;
using Defender.RiskGamesService.Application.Common.Interfaces.Services.Lottery;
using Defender.RiskGamesService.Application.Common.Interfaces.Services.Transaction;
using Defender.RiskGamesService.Application.Models.Lottery.Tickets;
using Defender.RiskGamesService.Application.Models.Transaction;
using Defender.RiskGamesService.Application.Services.Lottery.Tickets;
using Defender.RiskGamesService.Domain.Entities.Lottery.Draw;
using Defender.RiskGamesService.Domain.Entities.Lottery.TicketsSettings;
using Defender.RiskGamesService.Domain.Entities.Lottery.UserTickets;
using Defender.RiskGamesService.Domain.Enums;

namespace Defender.RiskGamesService.Tests.Services;

public class UserTicketManagementServiceCoverageTests
{
    [Fact]
    public async Task PurchaseTicketsAsync_WhenRequestNullOrEmpty_ReturnsEmpty()
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var nullResult = await sut.PurchaseTicketsAsync(null!);
        var emptyResult = await sut.PurchaseTicketsAsync(new PurchaseLotteryTicketsRequest { DrawNumber = 1, Amount = 100, Currency = Currency.USD, TicketNumbers = [] });

        Assert.Empty(nullResult);
        Assert.Empty(emptyResult);
    }

    [Fact]
    public async Task PurchaseTicketsAsync_WhenDrawNotActive_ThrowsServiceException()
    {
        var sut = CreateSut(out _, out var lotteryService, out _, out _);
        lotteryService.Setup(x => x.GetLotteryDrawByNumberAsync(1)).ReturnsAsync(new LotteryDraw
        {
            DrawNumber = 1,
            StartDate = DateTime.UtcNow.AddDays(-2),
            EndDate = DateTime.UtcNow.AddDays(-1)
        });

        await Assert.ThrowsAsync<ServiceException>(() => sut.PurchaseTicketsAsync(new PurchaseLotteryTicketsRequest
        {
            DrawNumber = 1,
            Amount = 100,
            Currency = Currency.USD,
            TicketNumbers = [1, 2]
        }));
    }

    [Fact]
    public async Task PurchaseTicketsAsync_WhenValid_CreatesTicketsAndStartsTransaction()
    {
        var sut = CreateSut(out var accountAccessor, out var lotteryService, out var ticketRepository, out var transactionService);
        var userId = Guid.NewGuid();
        accountAccessor.Setup(x => x.GetAccountId()).Returns(userId);
        lotteryService.Setup(x => x.GetLotteryDrawByNumberAsync(7)).ReturnsAsync(new LotteryDraw
        {
            DrawNumber = 7,
            StartDate = DateTime.UtcNow.AddMinutes(-10),
            EndDate = DateTime.UtcNow.AddMinutes(30),
            AllowedCurrencies = [Currency.USD],
            AllowedBets = [100],
            MinBetValue = 100,
            MaxBetValue = 100,
            IsCustomBetAllowed = false,
            MinTicketNumber = 1,
            MaxTicketNumber = 10
        });
        ticketRepository.Setup(x => x.GetUserTicketsByDrawNumberAsync(7)).ReturnsAsync([]);
        ticketRepository.Setup(x => x.CreateUserTicketsAsync(It.IsAny<List<UserTicket>>()))
            .ReturnsAsync((List<UserTicket> t) => t);
        transactionService.Setup(x => x.StartTransactionAsync(It.IsAny<TransactionRequest>()))
            .ReturnsAsync(new StartTransactionResult(
                new TransactionModel { TransactionId = "tx-777", TransactionType = TransactionType.Payment, TransactionPurpose = TransactionPurpose.Lottery },
                Task.CompletedTask));

        var result = await sut.PurchaseTicketsAsync(new PurchaseLotteryTicketsRequest
        {
            DrawNumber = 7,
            Amount = 100,
            Currency = Currency.USD,
            TicketNumbers = [1, 2, 3]
        });

        Assert.Equal(3, result.Count());
        ticketRepository.Verify(x => x.CreateUserTicketsAsync(It.IsAny<List<UserTicket>>()), Times.Once);
    }

    [Fact]
    public async Task CheckWinningsAsync_WhenDrawNullOrNoWinnings_ReturnsWithoutCalls()
    {
        var sut = CreateSut(out _, out _, out var ticketRepository, out _);

        await sut.CheckWinningsAsync(null!);
        await sut.CheckWinningsAsync(new LotteryDraw { Winnings = [] });

        ticketRepository.Verify(x => x.UpdateUserTicketAsync(It.IsAny<Defender.Common.DB.Model.UpdateModelRequest<UserTicket>>()), Times.Never);
    }

    private static UserTicketManagementService CreateSut(
        out Mock<ICurrentAccountAccessor> currentAccountAccessor,
        out Mock<ILotteryManagementService> lotteryManagementService,
        out Mock<ILotteryUserTicketRepository> userTicketRepository,
        out Mock<ITransactionManagementService> transactionManagementService)
    {
        currentAccountAccessor = new Mock<ICurrentAccountAccessor>();
        lotteryManagementService = new Mock<ILotteryManagementService>();
        userTicketRepository = new Mock<ILotteryUserTicketRepository>();
        transactionManagementService = new Mock<ITransactionManagementService>();

        return new UserTicketManagementService(
            currentAccountAccessor.Object,
            lotteryManagementService.Object,
            userTicketRepository.Object,
            transactionManagementService.Object);
    }
}
