using Defender.TravelCalendarService.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Defender.TravelCalendarService.Tests.Domain;

public class TravelCalendarMongoCompatibilityTests
{
    [Fact]
    public void Deserialize_WhenLegacyEventsFieldExists_IgnoresTheRemovedField()
    {
        var document = new BsonDocument
        {
            ["_id"] = Guid.NewGuid().ToString(),
            ["UserId"] = Guid.NewGuid().ToString(),
            ["Events"] = new BsonArray(),
        };

        var calendar = BsonSerializer.Deserialize<TravelCalendar>(document);

        Assert.NotEqual(Guid.Empty, calendar.UserId);
    }
}
