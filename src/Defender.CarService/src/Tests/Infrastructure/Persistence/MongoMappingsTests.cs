using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Enums;
using Defender.CarService.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Defender.CarService.Tests.Infrastructure.Persistence;

public sealed class MongoMappingsTests
{
    [Fact]
    public void ServiceHistory_WhenSerialized_UsesStringEnumUtcDateAndOmittedNulls()
    {
        MongoMappings.Register();
        var vehicleId = Guid.NewGuid();
        var history = ServiceHistoryRecord.Create(
            Guid.NewGuid(),
            vehicleId,
            new DateOnly(2026, 9, 13),
            123_456,
            HistoryType.Repair,
            "Brake repair",
            costAmountMinor: 12_345,
            costCurrency: Currency.PLN,
            timeProvider: TimeProvider.System);

        var document = history.ToBsonDocument();

        Assert.Equal(BsonType.String, document[nameof(ServiceHistoryRecord.Type)].BsonType);
        Assert.Equal(nameof(HistoryType.Repair), document[nameof(ServiceHistoryRecord.Type)].AsString);
        Assert.Equal(BsonType.DateTime, document[nameof(ServiceHistoryRecord.Date)].BsonType);
        Assert.Equal(DateTimeKind.Utc, document[nameof(ServiceHistoryRecord.Date)].ToUniversalTime().Kind);
        Assert.Equal(12_345, document[nameof(ServiceHistoryRecord.CostAmountMinor)].AsInt64);
        Assert.Equal(nameof(Currency.PLN), document[nameof(ServiceHistoryRecord.CostCurrency)].AsString);
        Assert.False(document.Contains(nameof(ServiceHistoryRecord.Notes)));

        var roundTrip = BsonSerializer.Deserialize<ServiceHistoryRecord>(document);

        Assert.Equal(history.Date, roundTrip.Date);
        Assert.Equal(history.Type, roundTrip.Type);
        Assert.Equal(history.CostAmountMinor, roundTrip.CostAmountMinor);
        Assert.Equal(history.CostCurrency, roundTrip.CostCurrency);
        Assert.Null(roundTrip.Notes);
    }

    [Fact]
    public void AllPersistedEntities_WhenSerialized_ContainOnlyMappedFields()
    {
        MongoMappings.Register();
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var maintenanceId = Guid.NewGuid();
        var vehicle = Vehicle.Create(userId, "Garage", "Make", "Model", 2020, "PLATE", timeProvider: TimeProvider.System);
        var maintenance = MaintenanceItem.Create(
            userId,
            vehicleId,
            "Oil",
            12,
            null,
            new DateOnly(2026, 1, 1),
            null,
            TimeProvider.System,
            maintenanceId);
        var insurance = InsurancePolicy.Create(
            userId,
            vehicleId,
            "Provider",
            null,
            null,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            timeProvider: TimeProvider.System);
        var history = ServiceHistoryRecord.Create(
            userId,
            vehicleId,
            new DateOnly(2026, 9, 13),
            10,
            HistoryType.Maintenance,
            "Oil change",
            linkedMaintenanceItemIds: [maintenanceId]);

        var vehicleDocument = vehicle.ToBsonDocument();
        var maintenanceDocument = maintenance.ToBsonDocument();
        var historyDocument = history.ToBsonDocument();
        var insuranceDocument = insurance.ToBsonDocument();

        Assert.False(vehicleDocument.Contains(nameof(Vehicle.IsArchived)));
        Assert.False(maintenanceDocument.Contains(nameof(MaintenanceItem.ManualBaseline)));
        Assert.False(maintenanceDocument.Contains(nameof(MaintenanceItem.EffectiveBaseline)));
        Assert.Equal(maintenanceId, historyDocument[nameof(ServiceHistoryRecord.LinkedMaintenanceItemIds)][0].AsGuid);
        Assert.False(insuranceDocument.Contains(nameof(InsurancePolicy.Provider) + "Status"));
        Assert.False(insuranceDocument.Contains(nameof(InsurancePolicy.PolicyNumber)));
        Assert.False(insuranceDocument.Contains(nameof(InsurancePolicy.Notes)));
    }
}
