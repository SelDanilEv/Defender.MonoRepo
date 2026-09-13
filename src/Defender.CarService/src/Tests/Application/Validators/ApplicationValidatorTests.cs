using Defender.CarService.Application.Requests.History;
using Defender.CarService.Application.Requests.Insurance;
using Defender.CarService.Application.Requests.Maintenance;
using Defender.CarService.Application.Requests.Vehicles;
using Defender.CarService.Domain.Enums;

namespace Defender.CarService.Tests.Application.Validators;

public sealed class ApplicationValidatorTests
{
    [Fact]
    public async Task CreateVehicle_WhenDisplayNameBlank_ReturnsStableCode()
    {
        var request = new CreateVehicleCommand
        {
            DisplayName = " ",
            Make = "BMW",
            Model = "E46",
            Year = 2002,
            Plate = "ABC-123",
        };

        var result = await new CreateVehicleCommandValidator().ValidateAsync(request);

        Assert.Contains(result.Errors, error => error.ErrorMessage == "CAR_VEHICLE_DISPLAY_NAME_REQUIRED");
    }

    [Fact]
    public async Task CreateMaintenance_WhenBothIntervalsMissing_ReturnsStableCode()
    {
        var request = new CreateMaintenanceItemCommand
        {
            VehicleId = Guid.NewGuid(),
            Name = "Oil",
        };

        var result = await new CreateMaintenanceItemCommandValidator().ValidateAsync(request);

        Assert.Contains(result.Errors, error => error.ErrorMessage == "CAR_MAINTENANCE_INTERVAL_REQUIRED");
    }

    [Fact]
    public async Task CreateHistory_WhenCostPairIncomplete_ReturnsStableCode()
    {
        var request = new CreateHistoryCommand
        {
            VehicleId = Guid.NewGuid(),
            Date = new DateOnly(2026, 1, 1),
            OdometerKm = 10,
            Type = HistoryType.Repair,
            Title = "Repair",
            CostAmountMinor = 100,
        };

        var result = await new CreateHistoryCommandValidator().ValidateAsync(request);

        var failure = Assert.Single(result.Errors, error => error.ErrorMessage == "CAR_HISTORY_COST_PAIR_INVALID");
        Assert.Equal(nameof(CreateHistoryCommand.CostAmountMinor), failure.PropertyName);
    }

    [Fact]
    public async Task GetHistory_WhenPageSizeExceedsMaximum_ReturnsStableCode()
    {
        var request = new GetHistoryQuery
        {
            VehicleId = Guid.NewGuid(),
            Page = 0,
            PageSize = 101,
        };

        var result = await new GetHistoryQueryValidator().ValidateAsync(request);

        Assert.Contains(result.Errors, error => error.ErrorMessage == "CAR_HISTORY_PAGINATION_INVALID");
    }

    [Fact]
    public async Task CreateInsurance_WhenStartAfterEnd_ReturnsStableCode()
    {
        var request = new CreateInsurancePolicyCommand
        {
            VehicleId = Guid.NewGuid(),
            Provider = "Provider",
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 1, 1),
        };

        var result = await new CreateInsurancePolicyCommandValidator().ValidateAsync(request);

        Assert.Contains(result.Errors, error => error.ErrorMessage == "CAR_INSURANCE_DATE_RANGE_INVALID");
    }
}
