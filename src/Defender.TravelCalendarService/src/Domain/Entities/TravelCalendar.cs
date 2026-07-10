using Defender.Common.Entities;
using Defender.TravelCalendarService.Domain.Exceptions;
using Defender.TravelCalendarService.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Defender.TravelCalendarService.Domain.Entities;

[BsonIgnoreExtraElements]
public class TravelCalendar : IBaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }
    public long Version { get; set; }
    public string BaseCity { get; set; } = "Warsaw";
    public string Currency { get; set; } = "PLN";
    public DateOnly SeasonStart { get; set; } = new(2026, 7, 1);
    public DateOnly SeasonEnd { get; set; } = new(2026, 9, 30);
    public CalendarTheme Theme { get; set; } = CalendarTheme.Light;
    public VehicleSettings Vehicle { get; set; } = new("Dodge Challenger", 12, 6.60m);
    public List<CalendarHoliday> Holidays { get; set; } = [];
    public List<PackingItem> PackingItems { get; set; } = [];
    public bool HasSeededEvents { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public static TravelCalendar Create(Guid userId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, CreatedAtUtc = now, UpdatedAtUtc = now,
    };

    public void SetTheme(CalendarTheme theme, DateTimeOffset now) { Theme = theme; Touch(now); }

    public Guid AddPackingItem(string text, DateTimeOffset now)
    {
        var item = new PackingItem { Text = Required(text, 120, "TRAVEL_CALENDAR_PACKING_REQUIRED"), Order = PackingItems.Count }; PackingItems.Add(item); Touch(now); return item.Id;
    }

    public void UpdatePackingItem(Guid itemId, string? text, bool? isChecked, DateTimeOffset now)
    {
        var item = PackingItems.FirstOrDefault(value => value.Id == itemId) ?? throw new TravelCalendarNotFoundException("Packing item was not found."); if (text != null) item.Text = Required(text, 120, "TRAVEL_CALENDAR_PACKING_REQUIRED"); if (isChecked != null) item.IsChecked = isChecked.Value; Touch(now);
    }

    public void RemovePackingItem(Guid itemId, DateTimeOffset now)
    {
        var item = PackingItems.FirstOrDefault(value => value.Id == itemId) ?? throw new TravelCalendarNotFoundException("Packing item was not found."); PackingItems.Remove(item); Touch(now);
    }

    private static string Required(string value, int max, string code) { var result = value.Trim(); if (result.Length == 0 || result.Length > max) throw new TravelCalendarValidationException(code, $"Text must contain 1-{max} characters."); return result; }
    private void Touch(DateTimeOffset now) { Version++; UpdatedAtUtc = now; }
}

public record CalendarHoliday(DateOnly Date, string NameKey, string Flag, string Type);
