using Defender.HealthCareService.Domain.Entities;
using Defender.HealthCareService.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Defender.HealthCareService.Tests.Domain;

public class HealthChartShareTests
{
    [Fact]
    public void Deserialize_WhenRangeModeIsMissing_PreservesLegacyDocumentCompatibility()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var document = new BsonDocument
        {
            { "Token", "legacy-token" },
            { "UserId", userId.ToString() },
        };

        var share = BsonSerializer.Deserialize<HealthChartShare>(document.ToBson());

        Assert.Equal("legacy-token", share.Token);
        Assert.Equal(userId, share.UserId);
        Assert.Null(share.RangeMode);
    }

    [Fact]
    public void Serialize_WhenRangeModeIsSet_StoresEnumAsString()
    {
        var share = new HealthChartShare
        {
            Token = "absolute-token",
            RangeMode = HealthChartShareRangeMode.Absolute,
        };

        var document = share.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<HealthChartShare>(document.ToBson());

        Assert.Equal("Absolute", document["RangeMode"].AsString);
        Assert.Equal(HealthChartShareRangeMode.Absolute, restored.RangeMode);
    }
}
