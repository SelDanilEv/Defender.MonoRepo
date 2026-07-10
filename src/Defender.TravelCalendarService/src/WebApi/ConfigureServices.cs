using System.Text;
using System.Text.Json.Serialization;
using Defender.Common.Consts;
using Defender.Common.Enums;
using Defender.Common.Helpers;
using Defender.Common.Extension;
using Defender.TravelCalendarService.Domain.Exceptions;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using HellangProblemDetailsOptions = Hellang.Middleware.ProblemDetails.ProblemDetailsOptions;

namespace Defender.TravelCalendarService.WebApi;

public static class ConfigureServices
{
    public static IServiceCollection AddTravelCalendarWebApi(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var signingKey = SecretsHelper.GetSecretSync(Secret.JwtSecret, true);
        if (string.IsNullOrWhiteSpace(signingKey) && environment.IsLocalOrDevelopment())
        {
            signingKey = configuration["JwtLocalDevelopmentKey"];
        }

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("JwtSecret is required outside Local/Development.");
        }

        services.AddHttpContextAccessor();
        services.AddCors(options => options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.AddProblemDetails(options => ConfigureProblems(options, environment));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; options.SaveToken = true;
            options.TokenValidationParameters = new() { NameClaimType = ClaimTypes.NameIdentifier, ValidateIssuer = true, ValidateAudience = false, ValidIssuer = configuration["JwtTokenIssuer"], ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)) };
        });
        services.AddAuthorization();
        services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull; });
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = false);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.UseInlineDefinitionsForEnums();
            options.SwaggerDoc("v1", new OpenApiInfo { Version = "v1", Title = "Travel Calendar Service", Description = "User-scoped travel calendar, trip planning, budgets, and packing list" });
        });
        return services;
    }

    private static void ConfigureProblems(HellangProblemDetailsOptions options, IWebHostEnvironment environment)
    {
        options.IncludeExceptionDetails = (_, _) => environment.IsLocalOrDevelopment();
        options.Map<TravelCalendarNotFoundException>(exception => Problem(exception, StatusCodes.Status404NotFound));
        options.Map<TravelCalendarConflictException>(exception => Problem(exception, StatusCodes.Status409Conflict));
        options.Map<TravelCalendarValidationException>(exception => Problem(exception, StatusCodes.Status422UnprocessableEntity));
        options.Map<Exception>(_ => new ProblemDetails { Status = 500, Detail = "UNHANDLED_ERROR" });
    }

    private static ProblemDetails Problem(TravelCalendarDomainException exception, int status) => new() { Status = status, Detail = exception.Message, Extensions = { ["code"] = exception.Code } };
}
