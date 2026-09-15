using System.Text.Json;
using Defender.CarService.WebApi;
using Defender.CarService.WebApi.Contracts;
using Defender.CarService.Domain.Enums;

namespace Defender.CarService.Tests.WebApi.Serialization;

public sealed class CarJsonSerializationTests
{
    [Fact]
    public void HistoryRequest_UsesCamelCaseIsoDateStringEnumAndPreservesNullCostPair()
    {
        var request = new CreateServiceHistoryRequest
        {
            Date = new DateOnly(2026, 9, 14),
            OdometerKm = 12_345,
            Type = HistoryType.Repair,
            Title = "Repair",
            LinkedMaintenanceItemIds = [],
            CostAmountMinor = null,
            CostCurrency = null,
        };

        var json = JsonSerializer.Serialize(request, CarJsonOptions.Create());

        Assert.Contains("\"date\":\"2026-09-14\"", json);
        Assert.Contains("\"type\":\"Repair\"", json);
        Assert.DoesNotContain("costAmountMinor", json);
        Assert.DoesNotContain("costCurrency", json);
    }

    [Theory]
    [InlineData("{\"type\":1}")]
    [InlineData("{\"type\":\"Mystery\"}")]
    public void HistoryRequest_RejectsNumericAndUnknownEnumWireValues(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateServiceHistoryRequest>(json, CarJsonOptions.Create()));
    }

    [Fact]
    public void VehicleRequests_DoNotExposeOwnershipOrServerFields()
    {
        var forbiddenNames = new[] { "Id", "UserId", "Archived", "CurrentOdometerKm", "Version", "CreatedAtUtc", "UpdatedAtUtc" };

        Assert.Empty(
            typeof(CreateVehicleRequest)
                .GetProperties()
                .Select(property => property.Name)
                .Intersect(forbiddenNames));
        Assert.Empty(
            typeof(UpdateVehicleRequest)
                .GetProperties()
                .Select(property => property.Name)
                .Intersect(forbiddenNames));
    }
}
