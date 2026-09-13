using Defender.CarService.Domain.Entities;
using Defender.CarService.Infrastructure.Persistence;
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
}
