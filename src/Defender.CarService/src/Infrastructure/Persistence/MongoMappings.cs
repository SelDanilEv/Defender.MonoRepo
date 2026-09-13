using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Defender.CarService.Infrastructure.Persistence;

public static class MongoMappings
{
    private static readonly object Sync = new();
    private static bool registered;

    public static void Register()
    {
        if (registered)
        {
            return;
        }

        lock (Sync)
        {
            if (registered)
            {
                return;
            }

            RegisterVehicle();
            RegisterMaintenanceItem();
            RegisterServiceHistoryRecord();
            RegisterInsurancePolicy();
            registered = true;
        }
    }

    private static void RegisterVehicle()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Vehicle)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<Vehicle>(map =>
        {
            map.MapIdMember(item => item.Id).SetSerializer(GuidSerializer);
            map.MapMember(item => item.UserId).SetSerializer(GuidSerializer);
            map.MapMember(item => item.DisplayName);
            map.MapMember(item => item.Make);
            map.MapMember(item => item.Model);
            map.MapMember(item => item.Year);
            map.MapMember(item => item.Plate);
            map.MapMember(item => item.Vin).SetIgnoreIfDefault(true);
            map.MapMember(item => item.Archived);
            map.MapMember(item => item.CurrentOdometerKm).SetIgnoreIfDefault(true);
            map.MapMember(item => item.CreatedAtUtc).SetSerializer(DateTimeOffsetSerializer);
            map.MapMember(item => item.UpdatedAtUtc).SetSerializer(DateTimeOffsetSerializer);
            map.MapMember(item => item.Version);
            map.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterMaintenanceItem()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(MaintenanceItem)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<MaintenanceItem>(map =>
        {
            map.MapIdMember(item => item.Id).SetSerializer(GuidSerializer);
            map.MapMember(item => item.UserId).SetSerializer(GuidSerializer);
            map.MapMember(item => item.VehicleId).SetSerializer(GuidSerializer);
            map.MapMember(item => item.Name);
            map.MapMember(item => item.IntervalMonths).SetIgnoreIfDefault(true);
            map.MapMember(item => item.IntervalThousandKm).SetIgnoreIfDefault(true);
            map.MapMember(item => item.ManualBaselineDate)
                .SetSerializer(NullableDateOnlySerializer)
                .SetIgnoreIfDefault(true);
            map.MapMember(item => item.ManualBaselineOdometerKm).SetIgnoreIfDefault(true);
            map.MapMember(item => item.LastDate)
                .SetSerializer(NullableDateOnlySerializer)
                .SetIgnoreIfDefault(true);
            map.MapMember(item => item.LastOdometerKm).SetIgnoreIfDefault(true);
            map.MapMember(item => item.CreatedAtUtc).SetSerializer(DateTimeOffsetSerializer);
            map.MapMember(item => item.UpdatedAtUtc).SetSerializer(DateTimeOffsetSerializer);
            map.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterServiceHistoryRecord()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(ServiceHistoryRecord)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<ServiceHistoryRecord>(map =>
        {
            map.MapIdMember(item => item.Id).SetSerializer(GuidSerializer);
            map.MapMember(item => item.UserId).SetSerializer(GuidSerializer);
            map.MapMember(item => item.VehicleId).SetSerializer(GuidSerializer);
            map.MapMember(item => item.Date).SetSerializer(DateOnlySerializer);
            map.MapMember(item => item.OdometerKm);
            map.MapMember(item => item.Type).SetSerializer(HistoryTypeSerializer);
            map.MapMember(item => item.Title);
            map.MapMember(item => item.Notes).SetIgnoreIfDefault(true);
            map.MapMember(item => item.CostAmountMinor).SetIgnoreIfDefault(true);
            map.MapMember(item => item.CostCurrency)
                .SetSerializer(NullableCurrencySerializer)
                .SetIgnoreIfDefault(true);
            map.MapMember(item => item.LinkedMaintenanceItemIds).SetSerializer(GuidListSerializer);
            map.MapMember(item => item.CreatedAtUtc).SetSerializer(DateTimeOffsetSerializer);
            map.MapMember(item => item.UpdatedAtUtc).SetSerializer(DateTimeOffsetSerializer);
            map.SetIgnoreExtraElements(true);
        });
    }

    private static void RegisterInsurancePolicy()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(InsurancePolicy)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<InsurancePolicy>(map =>
        {
            map.MapIdMember(item => item.Id).SetSerializer(GuidSerializer);
            map.MapMember(item => item.UserId).SetSerializer(GuidSerializer);
            map.MapMember(item => item.VehicleId).SetSerializer(GuidSerializer);
            map.MapMember(item => item.Provider);
            map.MapMember(item => item.PolicyNumber).SetIgnoreIfDefault(true);
            map.MapMember(item => item.CoverageType).SetIgnoreIfDefault(true);
            map.MapMember(item => item.StartDate).SetSerializer(DateOnlySerializer);
            map.MapMember(item => item.EndDate).SetSerializer(DateOnlySerializer);
            map.MapMember(item => item.Notes).SetIgnoreIfDefault(true);
            map.MapMember(item => item.CreatedAtUtc).SetSerializer(DateTimeOffsetSerializer);
            map.MapMember(item => item.UpdatedAtUtc).SetSerializer(DateTimeOffsetSerializer);
            map.SetIgnoreExtraElements(true);
        });
    }

    private static GuidSerializer GuidSerializer { get; } = new(GuidRepresentation.Standard);

    private static DateOnlyUtcMidnightSerializer DateOnlySerializer { get; } = new();

    private static NullableSerializer<DateOnly> NullableDateOnlySerializer { get; } =
        new(DateOnlySerializer);

    private static DateTimeOffsetSerializer DateTimeOffsetSerializer { get; } =
        new(BsonType.DateTime);

    private static EnumSerializer<HistoryType> HistoryTypeSerializer { get; } =
        new(BsonType.String);

    private static EnumSerializer<Currency> CurrencySerializer { get; } =
        new(BsonType.String);

    private static NullableSerializer<Currency> NullableCurrencySerializer { get; } =
        new(CurrencySerializer);

    private static IEnumerableDeserializingAsCollectionSerializer<IReadOnlyList<Guid>, Guid, List<Guid>> GuidListSerializer { get; } =
        new(GuidSerializer);
}

public sealed class DateOnlyUtcMidnightSerializer : SerializerBase<DateOnly>
{
    private static readonly DateTimeSerializer DateTimeSerializer = new(DateTimeKind.Utc);

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
    {
        DateTimeSerializer.Serialize(context, value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
    }

    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return DateOnly.FromDateTime(DateTimeSerializer.Deserialize(context));
    }
}
