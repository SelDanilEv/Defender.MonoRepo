using System.Text.Json;
using Defender.Common.Configuration.Options;
using Defender.Common.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Defender.Common.Extension;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddDefenderHealthChecks(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddHealthChecks();

        return services;
    }

    public static IServiceCollection AddMongoDbReadiness(this IServiceCollection services)
    {
        services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return new MongoClient(options.ConnectionString);
        });
        services.AddHealthChecks()
            .AddCheck<MongoDbHealthCheck>("mongodb", tags: ["ready"]);

        return services;
    }

    public static IEndpointRouteBuilder MapDefenderHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteHealthCheckResponseAsync
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponseAsync
        });

        return endpoints;
    }

    private static Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        return JsonSerializer.SerializeAsync(
            context.Response.Body,
            new HealthCheckDto(report.Status.ToString()),
            cancellationToken: context.RequestAborted);
    }

    private sealed class MongoDbHealthCheck(IMongoClient client) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await client
                    .GetDatabase("admin")
                    .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);

                return HealthCheckResult.Healthy();
            }
            catch (Exception exception)
            {
                return HealthCheckResult.Unhealthy("MongoDB is unavailable.", exception);
            }
        }
    }
}
