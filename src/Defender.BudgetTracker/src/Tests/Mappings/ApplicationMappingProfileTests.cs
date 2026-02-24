using AutoMapper;
using Defender.BudgetTracker.Application.DTOs;
using Defender.BudgetTracker.Application.Mappings;
using Defender.BudgetTracker.Application.Models.BudgetReview;
using Defender.BudgetTracker.Domain.Entities.Position;
using Defender.BudgetTracker.Domain.Entities.Reviews;
using Defender.BudgetTracker.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defender.BudgetTracker.Tests.Mappings;

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
}
