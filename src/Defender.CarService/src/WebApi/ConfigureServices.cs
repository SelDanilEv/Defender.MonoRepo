using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.CarService.Application.Common.Interfaces.Services;
using Defender.CarService.Application.Services;
using Defender.CarService.Infrastructure.Persistence;
using Defender.CarService.Infrastructure.Repositories;
using Defender.Common.Configuration.Options;
using Defender.Common.Enums;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Common.Extension;
using Defender.Common.Helpers;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hellang.Middleware.ProblemDetails;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Driver;
using ClaimTypes = Defender.Common.Consts.ClaimTypes;
using CommonValidationException = Defender.Common.Exceptions.ValidationException;
using ProblemDetailsOptions = Hellang.Middleware.ProblemDetails.ProblemDetailsOptions;

namespace Defender.CarService.WebApi;

public static class ConfigureServices
{
    public static IServiceCollection AddWebUIServices(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        services.AddCommonServices(configuration);
        services.AddHttpContextAccessor();
        services.AddProblemDetails(options => ConfigureProblemDetails(options, environment));
        services.AddJwtAuthentication(configuration, environment);
        services.AddSwagger();
        services.AddFluentValidationAutoValidation();
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
        services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);
        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(Defender.CarService.Application.AssemblyMarker).Assembly;

        services.AddAutoMapper(configuration => configuration.AddMaps(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(applicationAssembly));
        services.AddScoped<MyGarageApplicationService>();
        services.AddScoped<IMyGarageApplicationService>(serviceProvider =>
            serviceProvider.GetRequiredService<MyGarageApplicationService>());
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IMongoDatabase>(serviceProvider =>
        {
            var mongoOptions = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();

            return mongoClient.GetDatabase(mongoOptions.GetDatabaseName());
        });
        services.AddSingleton<MongoIndexInitializer>();
        services.AddSingleton<ICarTransactionCoordinator, MongoTransactionCoordinator>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IMaintenanceItemRepository, MaintenanceItemRepository>();
        services.AddScoped<IServiceHistoryRepository, ServiceHistoryRepository>();
        services.AddScoped<IInsurancePolicyRepository, InsurancePolicyRepository>();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services
            .AddAuthentication(authentication =>
            {
                authentication.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authentication.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = ClaimTypes.NameIdentifier,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = configuration["JwtTokenIssuer"],
                    ValidAudience = configuration["JwtTokenAudience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(GetJwtSigningKey(configuration, environment))),
                };
            });

        return services;
    }

    private static string GetJwtSigningKey(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var signingKey = IsLocalJwtConfiguration(environment)
            ? configuration["JwtLocalDevelopmentKey"]
            : null;

        signingKey = string.IsNullOrWhiteSpace(signingKey)
            ? SecretsHelper.GetSecretSync(Secret.JwtSecret, true)
            : signingKey;

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("JwtSecret is required outside Local/Debug.");
        }

        return signingKey;
    }

    private static bool IsLocalJwtConfiguration(IWebHostEnvironment environment) =>
        environment.IsEnvironment("Local") || environment.IsEnvironment("Debug");

    private static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.UseInlineDefinitionsForEnums();
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Car Service",
                    Description = "User-owned vehicle, maintenance, service history, and insurance service.",
                });
            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme.",
                });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document, null),
                    new List<string>()
                },
            });
        });

        return services;
    }

    private static void ConfigureProblemDetails(
        ProblemDetailsOptions options,
        IWebHostEnvironment environment)
    {
        options.IncludeExceptionDetails = (_, _) => environment.IsLocalOrDevelopment();

        options.Map<CommonValidationException>(exception =>
        {
            var validationProblemDetails = new ValidationProblemDetails(exception.Errors)
            {
                Detail = exception.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
            };

            return validationProblemDetails;
        });

        options.Map<ForbiddenAccessException>(exception => new ProblemDetails
        {
            Detail = exception.Message,
            Status = StatusCodes.Status403Forbidden,
        });

        options.Map<ServiceException>(exception => new ProblemDetails
        {
            Detail = exception.Message,
            Status = StatusCodes.Status400BadRequest,
        });

        options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);
        options.MapToStatusCode<HttpRequestException>(StatusCodes.Status503ServiceUnavailable);
        options.Map<Exception>(_ => new ProblemDetails
        {
            Detail = ErrorCodeHelper.GetErrorCode(ErrorCode.UnhandledError),
            Status = StatusCodes.Status500InternalServerError,
        });
    }
}
