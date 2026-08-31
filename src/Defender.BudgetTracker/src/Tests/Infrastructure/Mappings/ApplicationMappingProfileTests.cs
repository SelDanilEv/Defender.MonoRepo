using AutoMapper;
using Defender.BudgetTracker.Application.DTOs;
using Defender.BudgetTracker.Application.Mappings;
using Defender.BudgetTracker.Application.Models.BudgetReview;
using Defender.BudgetTracker.Domain.Entities.DiagramSetup;
using Defender.BudgetTracker.Domain.Entities.Position;
using Defender.BudgetTracker.Domain.Entities.RegularExpenses;
using Defender.BudgetTracker.Domain.Entities.Rates;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.BudgetTracker.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defender.BudgetTracker.Tests.Infrastructure.Mappings;

public class ApplicationMappingProfileTests
{
    private readonly IMapper _mapper;

    public ApplicationMappingProfileTests()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<Defender.BudgetTracker.Application.Mappings.MappingProfile>(),
            new NullLoggerFactory());
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Config_WhenCreated_IsValid()
    {
        Assert.NotNull(_mapper);
    }

    [Fact]
    public void Map_PositionToPublishToReviewedPosition_MapsCorrectly()
    {
        var source = new PositionToPublish
        {
            Name = "Salary",
            Currency = Currency.PLN,
            Amount = 5000,
            Tags = ["income"]
        };

        var result = _mapper.Map<ReviewedPosition>(source);

        Assert.Equal("Salary", result.Name);
        Assert.Equal(Currency.PLN, result.Currency);
        Assert.Equal(5000, result.Amount);
    }

    [Fact]
    public void Map_BudgetReviewToBudgetReviewDto_MapsCorrectly()
    {
        var source = new BudgetReview
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Positions = [ReviewedPosition.FromPosition(new BasePosition { Name = "X", Currency = Currency.USD }, 100)]
        };

        var result = _mapper.Map<BudgetReviewDto>(source);

        Assert.NotNull(result);
        Assert.Equal(source.Date, result.Date);
    }

    [Fact]
    public void Map_RegularExpenseReviewToDto_MapsSnapshotContribution()
    {
        var source = new RegularExpenseReview
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Month = new DateOnly(2026, 8, 27),
            RatesModel = new RatesModel { Date = new DateOnly(2026, 8, 1) },
            Expenses =
            [
                new ReviewedRegularExpense
                {
                    RegularExpenseId = Guid.NewGuid(),
                    Name = "Insurance",
                    Type = RegularExpenseType.Annual,
                    Currency = Currency.PLN,
                    Amount = 100
                }
            ]
        };

        var result = _mapper.Map<RegularExpenseReviewDto>(source);

        Assert.Equal(new DateOnly(2026, 8, 1), result.Month);
        Assert.Equal(100m / 12m, Assert.Single(result.Expenses).MonthlyContribution);
    }

    [Fact]
    public void Map_RegularExpenseSetupToDto_MapsIndependentFields()
    {
        var source = new RegularExpenseDiagramSetup
        {
            UserId = Guid.NewGuid(),
            MainCurrency = Currency.EUR,
            LastMonths = 12,
            EndMonth = new DateOnly(2026, 8, 27)
        };

        var result = _mapper.Map<RegularExpenseDiagramSetupDto>(source);

        Assert.Equal(source.UserId, result.UserId);
        Assert.Equal(Currency.EUR, result.MainCurrency);
        Assert.Equal(new DateOnly(2026, 8, 1), result.EndMonth);
    }
}
