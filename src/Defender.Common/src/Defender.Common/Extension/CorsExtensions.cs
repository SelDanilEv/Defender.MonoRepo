using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Defender.Common.Extension;

public static class CorsExtensions
{
    public const string DefenderCorsPolicy = "DefenderCors";

    public static IServiceCollection AddDefenderCors(this IServiceCollection services, IWebHostEnvironment environment)
    {
        var origins = new List<string>
        {
            "https://coded-by-danil.dev",
            "https://*.coded-by-danil.dev"
        };

        if (environment.IsLocalOrDevelopment())
        {
            origins.Add("http://localhost");
            origins.Add("https://localhost");
            origins.Add("http://localhost:3000");
            origins.Add("https://localhost:3000");
        }

        services.AddCors(options => options.AddPolicy(
            DefenderCorsPolicy,
            policy => policy
                .WithOrigins(origins.ToArray())
                .SetIsOriginAllowedToAllowWildcardSubdomains()
                .AllowAnyHeader()
                .AllowAnyMethod()));

        return services;
    }
}
