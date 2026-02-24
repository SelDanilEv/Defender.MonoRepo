using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApi;

namespace Defender.WalletService.Tests.Configuration;

public class WebApiConfigureServicesTests
{
    private const string SecretPrefix = "Defender_App_";

    [Fact]
    public void AddWebUIServices_WhenCalled_RegistersExpectedWebServices()
    {
        var jwtSecretKey = SecretPrefix + "JwtSecret";
        var mongoConnectionKey = SecretPrefix + "MongoDBConnectionString";
        Environment.SetEnvironmentVariable(jwtSecretKey, "01234567890123456789012345678901", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(mongoConnectionKey, "mongodb://localhost:27017", EnvironmentVariableTarget.Process);

        try
        {
            var values = new Dictionary<string, string?>
            {
                ["JwtTokenIssuer"] = "wallet-tests",
                ["MongoDbOptions:Environment"] = "local",
                ["ServiceOptions:Url"] = "http://wallet.local"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.EnvironmentName).Returns("Local");
            var services = new ServiceCollection();

            services.AddWebUIServices(environment.Object, configuration);

            using var provider = services.BuildServiceProvider();
            var jwtOptionsMonitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var jwtOptions = jwtOptionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);
            var swaggerOptions = provider.GetRequiredService<IOptions<SwaggerGenOptions>>().Value;
            var apiBehaviorOptions = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            var problemDetailsOptions = provider.GetRequiredService<IOptions<ProblemDetailsOptions>>().Value;

            Assert.Equal("wallet-tests", jwtOptions.TokenValidationParameters.ValidIssuer);
            Assert.NotNull(jwtOptions.TokenValidationParameters.IssuerSigningKey);
            Assert.NotNull(swaggerOptions);
            Assert.True(apiBehaviorOptions.SuppressModelStateInvalidFilter);
            Assert.NotNull(problemDetailsOptions);
        }
        finally
        {
            Environment.SetEnvironmentVariable(jwtSecretKey, null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(mongoConnectionKey, null, EnvironmentVariableTarget.Process);
        }
    }
}
