using Defender.Common.Configuration.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenIddict.Abstractions;

namespace Defender.Portal.WebUI.OAuth;

public static class PortalOAuthServiceCollectionExtensions
{
    public static IServiceCollection AddPortalOAuth(this IServiceCollection services, IConfiguration configuration)
    {
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
            .AddServer(options => options.UseAspNetCore())
            .AddValidation(options => options.UseLocalServer());

        services.AddAuthorizationBuilder()
            .AddPolicy(PortalOAuthScopes.Read, policy =>
                policy.RequireClaim(OpenIddictConstants.Claims.Scope, PortalOAuthScopes.Read));

        return services;
    }
}
