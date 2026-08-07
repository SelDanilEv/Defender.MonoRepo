using System.Text.Json.Serialization;

namespace Defender.Portal.Application.DTOs.HealthCare;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HealthChartShareRangeMode
{
    Rolling,
    Absolute,
    All,
}
