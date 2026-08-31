using System.Net.Http.Headers;
using AutoMapper;
using Defender.Common.Clients.BudgetTracker;
using Defender.Common.DB.Pagination;
using Defender.Common.Interfaces;
using Defender.Common.Wrapper.Internal;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using Defender.Portal.Application.Enums;
using Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses;
using Defender.Portal.Infrastructure.Clients.BudgetTracker;
using PortalRegularExpenseReviewItemRequest = Defender.Portal.Application.Models.ApiRequests.BugetTracker.RegularExpenses.RegularExpenseReviewItemRequest;

namespace Defender.Portal.Tests.Infrastructure.Clients;

public class BudgetTrackerRegularExpenseWrapperTests
{
    [Fact]
    public async Task CreateRegularExpenseAsync_WhenRequestUsesPortalEnums_MapsClientCommandAndUserAuth()
    {
        var client = new Mock<IBudgetTrackerServiceClient>();
        var authentication = CreateAuthenticationMock();
        var mapper = new Mock<IMapper>();
        var expected = new PortalRegularExpense { Name = "Rent" };
        var response = new RegularExpenseDto { Name = "Rent" };
        mapper.Setup(item => item.Map<PortalRegularExpense>(response)).Returns(expected);
        client
            .Setup(item => item.RegularExpensePOSTAsync(It.IsAny<CreateRegularExpenseCommand>()))
            .ReturnsAsync(response);
        var sut = new BudgetTrackerWrapper(authentication.Object, client.Object, mapper.Object);

        var result = await sut.CreateRegularExpenseAsync(new CreateRegularExpenseRequest
        {
            Name = "Rent",
            Type = RegularExpenseType.Annual,
            Currency = Currency.PLN,
            DefaultAmount = 12_000,
            OrderPriority = 4,
        });

        Assert.Same(expected, result);
        client.Verify(item => item.RegularExpensePOSTAsync(It.Is<CreateRegularExpenseCommand>(command =>
            command.Name == "Rent"
            && command.Type == CreateRegularExpenseCommandType.Annual
            && command.Currency == CreateRegularExpenseCommandCurrency.PLN
            && command.DefaultAmount == 12_000
            && command.OrderPriority == 4)), Times.Once);
        authentication.Verify(item => item.GetAuthenticationHeader(AuthorizationType.User), Times.Once);
    }

    [Fact]
    public async Task GetRegularExpenseReviewsByDateRangeAsync_WhenCalled_ForwardsBothMonths()
    {
        var client = new Mock<IBudgetTrackerServiceClient>();
        var authentication = CreateAuthenticationMock();
        var mapper = new Mock<IMapper>();
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 3, 1);
        var response = new List<RegularExpenseReviewDto>();
        var expected = new List<PortalRegularExpenseReview>();
        mapper
            .Setup(item => item.Map<List<PortalRegularExpenseReview>>(It.IsAny<ICollection<RegularExpenseReviewDto>>()))
            .Returns(expected);
        client
            .Setup(item => item.ByDateRange2Async(start, end))
            .ReturnsAsync(response);
        var sut = new BudgetTrackerWrapper(authentication.Object, client.Object, mapper.Object);

        var result = await sut.GetRegularExpenseReviewsByDateRangeAsync(start, end);

        Assert.Same(expected, result);
        client.Verify(item => item.ByDateRange2Async(start, end), Times.Once);
    }

    [Fact]
    public async Task PublishRegularExpenseReviewAsync_WhenCalled_PreservesMonthAndSnapshotAmounts()
    {
        var client = new Mock<IBudgetTrackerServiceClient>();
        var authentication = CreateAuthenticationMock();
        var mapper = new Mock<IMapper>();
        var response = new RegularExpenseReviewDto { Month = new DateOnly(2026, 8, 1) };
        var expected = new PortalRegularExpenseReview { Month = response.Month };
        mapper.Setup(item => item.Map<PortalRegularExpenseReview>(response)).Returns(expected);
        client
            .Setup(item => item.RegularExpenseReviewPOSTAsync(It.IsAny<PublishRegularExpenseReviewCommand>()))
            .ReturnsAsync(response);
        var reviewId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var sut = new BudgetTrackerWrapper(authentication.Object, client.Object, mapper.Object);

        var result = await sut.PublishRegularExpenseReviewAsync(new PublishRegularExpenseReviewRequest
        {
            Id = reviewId,
            Month = new DateOnly(2026, 8, 19),
            Expenses =
            [
                new PortalRegularExpenseReviewItemRequest
                {
                    RegularExpenseId = expenseId,
                    Amount = 1_250,
                },
            ],
        });

        Assert.Same(expected, result);
        client.Verify(item => item.RegularExpenseReviewPOSTAsync(It.Is<PublishRegularExpenseReviewCommand>(command =>
            command.Id == reviewId
            && command.Month == new DateOnly(2026, 8, 19)
            && command.Expenses != null
            && command.Expenses.Count == 1
            && command.Expenses.Single().RegularExpenseId == expenseId
            && command.Expenses.Single().Amount == 1_250)), Times.Once);
    }

    [Fact]
    public async Task UpdateRegularExpenseDiagramSetupAsync_WhenCalled_MapsCurrencyAndOptionalDate()
    {
        var client = new Mock<IBudgetTrackerServiceClient>();
        var authentication = CreateAuthenticationMock();
        var mapper = new Mock<IMapper>();
        var response = new RegularExpenseDiagramSetupDto { MainCurrency = RegularExpenseDiagramSetupDtoMainCurrency.EUR };
        var expected = new PortalRegularExpenseDiagramSetup { MainCurrency = Currency.EUR };
        mapper.Setup(item => item.Map<PortalRegularExpenseDiagramSetup>(response)).Returns(expected);
        client
            .Setup(item => item.RegularExpenseDiagramSetupPOSTAsync(It.IsAny<UpdateRegularExpenseDiagramSetupCommand>()))
            .ReturnsAsync(response);
        var sut = new BudgetTrackerWrapper(authentication.Object, client.Object, mapper.Object);

        var result = await sut.UpdateRegularExpenseDiagramSetupAsync(new UpdateRegularExpenseDiagramSetupRequest
        {
            MainCurrency = Currency.EUR,
            LastMonths = 6,
            EndMonth = new DateOnly(2026, 8, 1),
        });

        Assert.Same(expected, result);
        client.Verify(item => item.RegularExpenseDiagramSetupPOSTAsync(It.Is<UpdateRegularExpenseDiagramSetupCommand>(command =>
            command.MainCurrency == UpdateRegularExpenseDiagramSetupCommandMainCurrency.EUR
            && command.LastMonths == 6
            && command.EndMonth == new DateOnly(2026, 8, 1))), Times.Once);
    }

    private static Mock<IAuthenticationHeaderAccessor> CreateAuthenticationMock()
    {
        var authentication = new Mock<IAuthenticationHeaderAccessor>();
        authentication
            .Setup(item => item.GetAuthenticationHeader(AuthorizationType.User))
            .ReturnsAsync(new AuthenticationHeaderValue("Bearer", "token"));
        return authentication;
    }
}
