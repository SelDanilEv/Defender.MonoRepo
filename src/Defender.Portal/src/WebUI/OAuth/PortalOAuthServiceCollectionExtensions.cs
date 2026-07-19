using Defender.Common.Configuration.Options;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Defender.Portal.WebUI.OAuth;

public static class PortalOAuthServiceCollectionExtensions
{
    public static IServiceCollection AddPortalOAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var discoveryMetadata = PortalOAuthDiscoveryMetadata.Create(
            configuration.GetRequiredSection(PortalOAuthOptions.SectionName).Get<PortalOAuthOptions>()!);

        services
            .AddOptions<PortalOAuthOptions>()
            .Bind(configuration.GetRequiredSection(PortalOAuthOptions.SectionName))
            .Validate(options => options.Validate().Succeeded, "Portal OAuth configuration is invalid.");

        services.AddSingleton<IMongoDatabase>(serviceProvider =>
        {
            var mongoOptions = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return new MongoClient(mongoOptions.ConnectionString).GetDatabase(mongoOptions.GetDatabaseName());
        });

        services.ConfigureOptions<PortalOpenIddictServerOptions>();
        services
            .AddOpenIddict()
            .AddCore(options => options.UseMongoDb())
            .AddServer(options =>
            {
                options.UseAspNetCore().EnableAuthorizationEndpointPassthrough();
                options.AddEventHandler<OpenIddictServerEvents.HandleConfigurationRequestContext>(builder =>
                    builder.UseInlineHandler(context =>
                    {
                        foreach (var metadata in discoveryMetadata)
                        {
                            context.Metadata[metadata.Key] = metadata.Value;
                        }

                        return default;
                    }));
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(PortalOAuthScopes.Read, policy =>
                policy.RequireClaim(OpenIddictConstants.Claims.Scope, PortalOAuthScopes.Read));

        services.AddRateLimiter(options => options.AddPolicy(
            "oauth-registration",
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                })));

        return services;
    }
}
