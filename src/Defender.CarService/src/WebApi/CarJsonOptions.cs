using System.Text.Json;
using System.Text.Json.Serialization;

namespace Defender.CarService.WebApi;

public static class CarJsonOptions
{
    public static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        if (options.Converters.All(converter => converter is not JsonStringEnumConverter))
        {
            options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        }
    }

    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Configure(options);
        return options;
    }
}
