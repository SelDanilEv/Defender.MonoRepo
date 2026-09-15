using Defender.CarService.Domain.Entities;
using Defender.CarService.Infrastructure.Persistence;
using Defender.CarService.Infrastructure.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Defender.CarService.Tests.Infrastructure.Repositories;

public sealed class RepositoryFilterTests
{
    [Fact]
    public void OwnedEntityFilter_ContainsUserAndEntityIdentifiers()
    {
        MongoMappings.Register();
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var filter = MongoRepositoryBase<Vehicle>.CreateUserEntityFilter(userId, vehicleId);

        var rendered = filter.Render(new RenderArgs<Vehicle>(
            BsonSerializer.LookupSerializer<Vehicle>(),
            BsonSerializer.SerializerRegistry,
            new PathRenderArgs(null, false),
            false,
            false,
            false,
            default));

        Assert.Equal(userId, rendered[nameof(Vehicle.UserId)].AsGuid);
        Assert.Equal(vehicleId, rendered["_id"].AsGuid);
    }

    [Fact]
    public void VersionFilter_ContainsUserEntityAndExpectedVersion()
    {
        MongoMappings.Register();
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var filter = MongoRepositoryBase<Vehicle>.CreateUserEntityVersionFilter(userId, vehicleId, 7);

        var rendered = filter.Render(new RenderArgs<Vehicle>(
            BsonSerializer.LookupSerializer<Vehicle>(),
            BsonSerializer.SerializerRegistry,
            new PathRenderArgs(null, false),
            false,
            false,
            false,
            default));

        Assert.Equal(3, rendered.ElementCount);
        Assert.Equal(userId, rendered[nameof(Vehicle.UserId)].AsGuid);
        Assert.Equal(vehicleId, rendered["_id"].AsGuid);
        Assert.Equal(7, rendered[nameof(Vehicle.Version)].AsInt64);
    }

    [Fact]
    public void RepositoryFilters_ContainUserAndVehicleOwnershipAcrossAllCollections()
    {
        MongoMappings.Register();
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var vehicle = Render(VehicleRepository.CreateUserEntityFilter(userId, vehicleId));
        var maintenance = Render(MaintenanceItemRepository.CreateUserVehicleEntityFilter(userId, vehicleId, entityId));
        var history = Render(ServiceHistoryRepository.CreateUserVehicleEntityFilter(userId, vehicleId, entityId));
        var insurance = Render(InsurancePolicyRepository.CreateUserVehicleEntityFilter(userId, vehicleId, entityId));

        Assert.Equal(userId, vehicle["UserId"].AsGuid);
        Assert.Equal(vehicleId, vehicle["_id"].AsGuid);
        AssertOwnership(maintenance, userId, vehicleId, entityId);
        AssertOwnership(history, userId, vehicleId, entityId);
        AssertOwnership(insurance, userId, vehicleId, entityId);
    }

    private static void AssertOwnership(BsonDocument rendered, Guid userId, Guid vehicleId, Guid entityId)
    {
        Assert.Equal(userId, rendered["UserId"].AsGuid);
        Assert.Equal(vehicleId, rendered["VehicleId"].AsGuid);
        Assert.Equal(entityId, rendered["_id"].AsGuid);
    }

    private static BsonDocument Render<T>(FilterDefinition<T> filter)
        => filter.Render(new RenderArgs<T>(
            BsonSerializer.LookupSerializer<T>(),
            BsonSerializer.SerializerRegistry,
            new PathRenderArgs(null, false),
            false,
            false,
            false,
            default));
}
