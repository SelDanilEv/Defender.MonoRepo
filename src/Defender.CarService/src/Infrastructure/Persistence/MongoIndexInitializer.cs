using Defender.CarService.Domain.Entities;
using MongoDB.Driver;

namespace Defender.CarService.Infrastructure.Persistence;

public sealed record MongoIndexDefinition(string CollectionName, IReadOnlyList<string> Keys, string Name);

public sealed class MongoIndexInitializer
{
    private readonly IMongoDatabase database;
    private readonly object sync = new();
    private Task? initialization;

    public MongoIndexInitializer(IMongoDatabase database)
    {
        this.database = database;
        MongoMappings.Register();
    }

    public static IReadOnlyList<MongoIndexDefinition> GetDefinitions() =>
    [
        new(MongoCollections.Vehicles, ["UserId:1", "Archived:1", "UpdatedAtUtc:-1"], "ix_vehicles_user_archived_updated"),
        new(MongoCollections.MaintenanceItems, ["UserId:1", "VehicleId:1", "Name:1"], "ix_maintenance_user_vehicle_name"),
        new(MongoCollections.ServiceHistoryRecords, ["UserId:1", "VehicleId:1", "Date:-1", "OdometerKm:-1"], "ix_history_user_vehicle_date_odometer"),
        new(MongoCollections.ServiceHistoryRecords, ["UserId:1", "VehicleId:1", "LinkedMaintenanceItemIds:1"], "ix_history_user_vehicle_links"),
        new(MongoCollections.InsurancePolicies, ["UserId:1", "VehicleId:1", "EndDate:-1"], "ix_insurance_user_vehicle_end"),
    ];

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            initialization ??= CreateIndexesAsync(cancellationToken);
            return initialization;
        }
    }

    private async Task CreateIndexesAsync(CancellationToken cancellationToken)
    {
        var vehicleIndexes = database.GetCollection<Vehicle>(MongoCollections.Vehicles).Indexes;
        var maintenanceIndexes = database.GetCollection<MaintenanceItem>(MongoCollections.MaintenanceItems).Indexes;
        var historyIndexes = database.GetCollection<ServiceHistoryRecord>(MongoCollections.ServiceHistoryRecords).Indexes;
        var insuranceIndexes = database.GetCollection<InsurancePolicy>(MongoCollections.InsurancePolicies).Indexes;

        await Task.WhenAll(
            vehicleIndexes.CreateOneAsync(
                new CreateIndexModel<Vehicle>(
                    Builders<Vehicle>.IndexKeys
                        .Ascending(nameof(Vehicle.UserId))
                        .Ascending(nameof(Vehicle.Archived))
                        .Descending(nameof(Vehicle.UpdatedAtUtc)),
                    new CreateIndexOptions { Name = "ix_vehicles_user_archived_updated" }),
                options: null,
                cancellationToken: cancellationToken),
            maintenanceIndexes.CreateOneAsync(
                new CreateIndexModel<MaintenanceItem>(
                    Builders<MaintenanceItem>.IndexKeys
                        .Ascending(nameof(MaintenanceItem.UserId))
                        .Ascending(nameof(MaintenanceItem.VehicleId))
                        .Ascending(nameof(MaintenanceItem.Name)),
                    new CreateIndexOptions { Name = "ix_maintenance_user_vehicle_name" }),
                options: null,
                cancellationToken: cancellationToken),
            historyIndexes.CreateManyAsync(
                [
                    new CreateIndexModel<ServiceHistoryRecord>(
                        Builders<ServiceHistoryRecord>.IndexKeys
                            .Ascending(nameof(ServiceHistoryRecord.UserId))
                            .Ascending(nameof(ServiceHistoryRecord.VehicleId))
                            .Descending(nameof(ServiceHistoryRecord.Date))
                            .Descending(nameof(ServiceHistoryRecord.OdometerKm)),
                        new CreateIndexOptions { Name = "ix_history_user_vehicle_date_odometer" }),
                    new CreateIndexModel<ServiceHistoryRecord>(
                        Builders<ServiceHistoryRecord>.IndexKeys
                            .Ascending(nameof(ServiceHistoryRecord.UserId))
                            .Ascending(nameof(ServiceHistoryRecord.VehicleId))
                            .Ascending(nameof(ServiceHistoryRecord.LinkedMaintenanceItemIds)),
                        new CreateIndexOptions { Name = "ix_history_user_vehicle_links" }),
                ],
                cancellationToken: cancellationToken),
            insuranceIndexes.CreateOneAsync(
                new CreateIndexModel<InsurancePolicy>(
                    Builders<InsurancePolicy>.IndexKeys
                        .Ascending(nameof(InsurancePolicy.UserId))
                        .Ascending(nameof(InsurancePolicy.VehicleId))
                        .Descending(nameof(InsurancePolicy.EndDate)),
                    new CreateIndexOptions { Name = "ix_insurance_user_vehicle_end" }),
                options: null,
                cancellationToken: cancellationToken));
    }
}
