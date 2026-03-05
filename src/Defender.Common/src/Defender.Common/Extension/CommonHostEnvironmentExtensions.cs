using Defender.Common.Enums;
using Microsoft.Extensions.Hosting;

namespace Defender.Common.Extension;

public static class CommonHostEnvironmentExtensions
{
    public static bool IsLocalOrDevelopment(this IHostEnvironment hostEnvironment)
    {
        return hostEnvironment.IsEnvironment("Dev")
            || hostEnvironment.IsEnvironment("Debug")
            || hostEnvironment.IsEnvironment("Local");
    }

    public static AppEnvironment GetAppEnvironment(this IHostEnvironment hostEnvironment)
    {
        return hostEnvironment.EnvironmentName switch
        {
            "Prod" => AppEnvironment.prod,
            "Dev" => AppEnvironment.dev,
            "Debug" or "Local" => AppEnvironment.local,
            _ => AppEnvironment.local,
        };
    }
}
