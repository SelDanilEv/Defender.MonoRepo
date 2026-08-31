using AutoMapper;
using Defender.Common.Clients.BudgetTracker;
using Defender.Common.DB.Pagination;
using Defender.Portal.Application.DTOs.BudgetTracking.RegularExpenses;
using Defender.Portal.Application.Enums;
using Defender.Portal.Infrastructure.Mappings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defender.Portal.Tests.Infrastructure.Mappings;

public class RegularExpenseClientModelsProfileTests
{
    private readonly IMapper mapper = new MapperConfiguration(
        config => config.AddProfile<ClientModelsProfile>(),
        new NullLoggerFactory()).CreateMapper();

    [Fact]
    public void Map_RegularExpenseDefinition_MapsIndependentEnumsAndAmount()
    {
        var source = new RegularExpenseDto
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Rent",
            Type = RegularExpenseDtoType.Regular,
            Currency = RegularExpenseDtoCurrency.PLN,
            DefaultAmount = 250_00,
            OrderPriority = 7,
        };

        var result = mapper.Map<PortalRegularExpense>(source);

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(source.UserId, result.UserId);
        Assert.Equal(RegularExpenseType.Regular, result.Type);
        Assert.Equal(Currency.PLN, result.Currency);
        Assert.Equal(source.DefaultAmount, result.DefaultAmount);
    }

    [Fact]
    public void Map_RegularExpenseReview_PreservesMonthSnapshotsContributionAndRates()
    {
        var reviewId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var source = new RegularExpenseReviewDto
        {
            Id = reviewId,
            UserId = Guid.NewGuid(),
            Month = new DateOnly(2026, 8, 1),
            Expenses =
            [
                new ReviewedRegularExpenseDto
                {
                    RegularExpenseId = expenseId,
                    Name = "Insurance",
                    Type = ReviewedRegularExpenseDtoType.Annual,
                    Currency = ReviewedRegularExpenseDtoCurrency.EUR,
                    Amount = 12_000,
                    OrderPriority = 2,
                    MonthlyContribution = 1_000.5,
                },
            ],
            RatesModel = new RatesModel
            {
                Date = new DateOnly(2026, 8, 1),
                BaseCurrency = RatesModelBaseCurrency.EUR,
                Rates = new Rates { EUR = 1, PLN = 4.25 },
            },
        };

        var result = mapper.Map<PortalRegularExpenseReview>(source);

        var snapshot = Assert.Single(result.Expenses);
        Assert.Equal(reviewId, result.Id);
        Assert.Equal(source.Month, result.Month);
        Assert.Equal(expenseId, snapshot.RegularExpenseId);
        Assert.Equal(RegularExpenseType.Annual, snapshot.Type);
        Assert.Equal(Currency.EUR, snapshot.Currency);
        Assert.Equal(1_000.5m, snapshot.MonthlyContribution);
        Assert.Equal(Currency.EUR, result.RatesModel.BaseCurrency);
        Assert.Equal(4.25m, result.RatesModel.Rates[Currency.PLN]);
    }

    [Fact]
    public void Map_RegularExpensePagedResult_MapsItemsAndPaging()
    {
        var source = new RegularExpenseDtoPagedResult
        {
            TotalItemsCount = 23,
            CurrentPage = 2,
            PageSize = 10,
            TotalPagesCount = 3,
            Items =
            [
                new RegularExpenseDto
                {
                    Type = RegularExpenseDtoType.Subscription,
                    Currency = RegularExpenseDtoCurrency.USD,
                    Name = "Cloud",
                },
            ],
        };

        var result = mapper.Map<PagedResult<PortalRegularExpense>>(source);

        Assert.Equal(source.TotalItemsCount, result.TotalItemsCount);
        Assert.Equal(source.CurrentPage, result.CurrentPage);
        Assert.Equal(source.PageSize, result.PageSize);
        Assert.Equal(source.TotalPagesCount, result.TotalPagesCount);
        Assert.Equal(RegularExpenseType.Subscription, Assert.Single(result.Items).Type);
    }

    [Fact]
    public void Map_RegularExpenseDiagramSetup_MapsOptionalFieldsAndCurrency()
    {
        var source = new RegularExpenseDiagramSetupDto
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            MainCurrency = RegularExpenseDiagramSetupDtoMainCurrency.USD,
            LastMonths = 12,
            EndMonth = new DateOnly(2026, 8, 1),
        };

        var result = mapper.Map<PortalRegularExpenseDiagramSetup>(source);

        Assert.Equal(source.Id, result.Id);
        Assert.Equal(Currency.USD, result.MainCurrency);
        Assert.Equal(source.LastMonths, result.LastMonths);
        Assert.Equal(source.EndMonth, result.EndMonth);
    }
}
