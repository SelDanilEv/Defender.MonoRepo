using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Defender.Common.Enums;
using Defender.IdentityService.Application.Common.Interfaces.Services;
using Defender.IdentityService.Application.Services;
using Defender.IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApi;

namespace Defender.IdentityService.Tests.Services;

public class TokenManagementServiceTests
{
    private const string JwtSecret = "0123456789ABCDEF0123456789ABCDEF";
    private const string JwtIssuer = "identity-tests";
    private const string JwtAudience = "defender-api";

    [Fact]
    public async Task GenerateNewJwtAsync_WhenAudienceConfigured_AddsConfiguredAudience()
    {
        Environment.SetEnvironmentVariable("Defender_App_JwtSecret", JwtSecret, EnvironmentVariableTarget.Process);

        try
        {
            var configuration = CreateConfiguration();
            var loginHistoryService = new Mock<ILoginHistoryService>();
            loginHistoryService
                .Setup(x => x.AddLoginRecordAsync(It.IsAny<LoginRecord>()))
                .ReturnsAsync(new LoginRecord());
            var service = new TokenManagementService(configuration, loginHistoryService.Object);

            var token = await service.GenerateNewJWTAsync(new AccountInfo { Id = Guid.NewGuid() });
            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Contains(JwtAudience, parsed.Audiences);
        }
        finally
        {
            Environment.SetEnvironmentVariable("Defender_App_JwtSecret", null, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    public void AddWebApiServices_WhenTokensHaveAudience_OnlyAcceptsConfiguredAudience()
    {
        Environment.SetEnvironmentVariable("Defender_App_JwtSecret", JwtSecret, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(
            "Defender_App_MongoDBConnectionString",
            "mongodb://localhost:27017",
            EnvironmentVariableTarget.Process);

        try
        {
            var services = new ServiceCollection();
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.EnvironmentName).Returns("Local");
            services.AddWebApiServices(environment.Object, CreateConfiguration());

            using var provider = services.BuildServiceProvider();
            var options = provider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            var handler = new JwtSecurityTokenHandler();

            ValidateToken(handler, CreateToken(JwtAudience), options.TokenValidationParameters);
            Assert.Throws<SecurityTokenInvalidAudienceException>(() =>
                ValidateToken(handler, CreateToken(null), options.TokenValidationParameters));
            Assert.Throws<SecurityTokenInvalidAudienceException>(() =>
                ValidateToken(handler, CreateToken("wrong-audience"), options.TokenValidationParameters));
        }
        finally
        {
            Environment.SetEnvironmentVariable("Defender_App_JwtSecret", null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("Defender_App_MongoDBConnectionString", null, EnvironmentVariableTarget.Process);
        }
    }

    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["JwtTokenIssuer"] = JwtIssuer,
                ["JwtTokenAudience"] = JwtAudience,
            })
        .Build();

    private static string CreateToken(string? audience)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            JwtIssuer,
            audience,
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void ValidateToken(
        JwtSecurityTokenHandler handler,
        string token,
        TokenValidationParameters parameters) => handler.ValidateToken(token, parameters, out _);
}
