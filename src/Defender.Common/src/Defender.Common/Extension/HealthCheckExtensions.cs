using System.Text.Json;
using Defender.Common.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Defender.Common.Extension;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddDefenderHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();

        return services;
    }

    public static IEndpointRouteBuilder MapDefenderHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
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
}
