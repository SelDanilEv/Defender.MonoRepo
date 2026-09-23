using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Defender.CarService.Application.Common;
using Defender.CarService.Application.Common.Exceptions;
using Defender.CarService.Application.Common.Interfaces.Services;
using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Requests.Maintenance;
using Defender.CarService.Application.Requests.History;
using Defender.CarService.Application.Requests.Insurance;
using Defender.CarService.Application.Requests.Vehicles;
using Defender.CarService.Domain.Enums;
using Defender.CarService.Domain.Exceptions;
using Defender.Common.Consts;
using Defender.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Defender.CarService.Tests.WebApi.Integration;

public sealed class CarWebApiPipelineTests
{
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000101");
    private static readonly Guid OtherUserVehicleId = Guid.Parse("00000000-0000-0000-0000-000000000201");

    [Fact]
    public async Task Vehicles_WithoutBearer_ReturnsAuthenticationChallenge()
    {
        using var factory = new CarWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/V1/car/vehicles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(response.Headers.WwwAuthenticate);
    }

    [Fact]
    public async Task Vehicles_WithJwt_ReturnsCamelCaseShapeAndUsesJwtAccount()
    {
        using var factory = new CarWebApplicationFactory();
        var accountAccessor = factory.Services.GetRequiredService<ICurrentAccountAccessor>();
        var accountId = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.ApplicationService
            .Setup(service => service.GetVehiclesAsync(It.IsAny<GetVehiclesQuery>(), It.IsAny<CancellationToken>()))
            .Returns((GetVehiclesQuery _, CancellationToken _) =>
            {
                accountId.TrySetResult(accountAccessor.GetAccountId());
                return Task.FromResult(new VehiclePageDto
                {
                    Items =
                    [
                        new VehicleSummaryDto
                        {
                            Id = OtherUserVehicleId,
                            DisplayName = "Daily",
                            Make = "Make",
                            Model = "Model",
                            Year = 2026,
                            Plate = "ABC",
                        },
                    ],
                    TotalItemsCount = 1,
                    CurrentPage = 0,
                    PageSize = 25,
                    TotalPagesCount = 1,
                });
            });

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.GetAsync("/api/V1/car/vehicles");
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(UserId, await accountId.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(1, document.RootElement.GetProperty("totalItemsCount").GetInt32());
        var vehicle = document.RootElement.GetProperty("items")[0];
        Assert.Equal("Daily", vehicle.GetProperty("displayName").GetString());
        Assert.True(vehicle.TryGetProperty("maintenanceCounts", out _));
        Assert.False(vehicle.TryGetProperty("DisplayName", out _));
    }

    [Fact]
    public async Task Vehicles_WithPageAndPageSizeQuery_PassesPagingToApplicationService()
    {
        using var factory = new CarWebApplicationFactory();
        GetVehiclesQuery? capturedQuery = null;
        factory.ApplicationService
            .Setup(service => service.GetVehiclesAsync(It.IsAny<GetVehiclesQuery>(), It.IsAny<CancellationToken>()))
            .Returns((GetVehiclesQuery query, CancellationToken _) =>
            {
                capturedQuery = query;
                return Task.FromResult(new VehiclePageDto { Items = [], TotalItemsCount = 0, CurrentPage = query.Page, PageSize = query.PageSize });
            });

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.GetAsync("/api/V1/car/vehicles?includeArchived=true&page=2&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedQuery);
        Assert.True(capturedQuery!.IncludeArchived);
        Assert.Equal(2, capturedQuery.Page);
        Assert.Equal(10, capturedQuery.PageSize);
    }

    [Fact]
    public async Task CreateVehicle_ReturnsCreatedResponseWithVehicleLocation()
    {
        using var factory = new CarWebApplicationFactory();
        var vehicleId = Guid.NewGuid();
        factory.ApplicationService
            .Setup(service => service.CreateVehicleAsync(
                It.IsAny<CreateVehicleCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VehicleDto { Id = vehicleId, DisplayName = "Daily" });

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.PostAsJsonAsync(
            "/api/V1/car/vehicles",
            new
            {
                displayName = "Daily",
                make = "Make",
                model = "Model",
                year = 2026,
                plate = "ABC",
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task CreateMaintenanceItem_ReturnsCreatedResponseWithMaintenanceLocation()
    {
        using var factory = new CarWebApplicationFactory();
        var vehicleId = Guid.NewGuid();
        var maintenanceId = Guid.NewGuid();
        factory.ApplicationService
            .Setup(service => service.CreateMaintenanceItemAsync(
                It.IsAny<CreateMaintenanceItemCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaintenanceItemDto { Id = maintenanceId, VehicleId = vehicleId, Name = "Oil" });

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.PostAsJsonAsync(
            $"/api/V1/car/vehicles/{vehicleId}/maintenance",
            new
            {
                name = "Oil",
                intervalMonths = 12,
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}/maintenance", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task CreateHistory_ReturnsCreatedResponseWithHistoryLocationAndBody()
    {
        using var factory = new CarWebApplicationFactory();
        var vehicleId = Guid.NewGuid();
        var historyId = Guid.NewGuid();
        factory.ApplicationService
            .Setup(service => service.CreateHistoryAsync(
                It.IsAny<CreateHistoryCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceHistoryRecordDto
            {
                Id = historyId,
                VehicleId = vehicleId,
                Date = new DateOnly(2026, 9, 15),
                OdometerKm = 51_000,
                Type = HistoryType.Repair,
                Title = "Brake repair",
                LinkedMaintenanceItemIds = [],
                CostAmountMinor = 0,
                CostCurrency = Currency.PLN,
            });

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.PostAsJsonAsync(
            $"/api/V1/car/vehicles/{vehicleId}/history",
            new
            {
                date = "2026-09-15",
                odometerKm = 51_000,
                type = "Repair",
                title = "Brake repair",
                costAmountMinor = 0,
                costCurrency = "PLN",
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}/history", response.Headers.Location?.AbsolutePath);
        Assert.Equal("?page=0&pageSize=25", response.Headers.Location?.Query);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(historyId, document.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(vehicleId, document.RootElement.GetProperty("vehicleId").GetGuid());
        Assert.Equal("Brake repair", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(0, document.RootElement.GetProperty("costAmountMinor").GetInt64());
        Assert.Equal("PLN", document.RootElement.GetProperty("costCurrency").GetString());
        factory.ApplicationService.Verify(service => service.CreateHistoryAsync(
            It.Is<CreateHistoryCommand>(command =>
                command.VehicleId == vehicleId
                && command.OdometerKm == 51_000
                && command.Type == HistoryType.Repair
                && command.CostAmountMinor == 0
                && command.CostCurrency == Currency.PLN),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateInsurancePolicy_ReturnsCreatedResponseWithInsuranceLocationAndBody()
    {
        using var factory = new CarWebApplicationFactory();
        var vehicleId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        factory.ApplicationService
            .Setup(service => service.CreateInsurancePolicyAsync(
                It.IsAny<CreateInsurancePolicyCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InsurancePolicyDto
            {
                Id = policyId,
                VehicleId = vehicleId,
                Provider = "Safe Cover",
                PolicyNumber = "P-1",
                CoverageType = "OC",
                StartDate = new DateOnly(2026, 9, 1),
                EndDate = new DateOnly(2027, 9, 1),
                Status = InsuranceStatus.Active,
            });

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.PostAsJsonAsync(
            $"/api/V1/car/vehicles/{vehicleId}/insurance",
            new
            {
                provider = "Safe Cover",
                policyNumber = "P-1",
                coverageType = "OC",
                startDate = "2026-09-01",
                endDate = "2027-09-01",
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}/insurance", response.Headers.Location?.AbsolutePath);
        Assert.Equal("", response.Headers.Location?.Query);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(policyId, document.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(vehicleId, document.RootElement.GetProperty("vehicleId").GetGuid());
        Assert.Equal("Safe Cover", document.RootElement.GetProperty("provider").GetString());
        Assert.Equal("P-1", document.RootElement.GetProperty("policyNumber").GetString());
        factory.ApplicationService.Verify(service => service.CreateInsurancePolicyAsync(
            It.Is<CreateInsurancePolicyCommand>(command =>
                command.VehicleId == vehicleId
                && command.Provider == "Safe Cover"
                && command.PolicyNumber == "P-1"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteInsurancePolicy_ReturnsNoContentResponse()
    {
        using var factory = new CarWebApplicationFactory();
        var vehicleId = Guid.NewGuid();
        var insuranceId = Guid.NewGuid();
        factory.ApplicationService
            .Setup(service => service.DeleteInsurancePolicyAsync(
                It.IsAny<DeleteInsurancePolicyCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.DeleteAsync($"/api/V1/car/vehicles/{vehicleId}/insurance/{insuranceId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        factory.ApplicationService.Verify(service => service.DeleteInsurancePolicyAsync(
            It.Is<DeleteInsurancePolicyCommand>(command => command.VehicleId == vehicleId && command.InsuranceId == insuranceId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VehicleForOtherAccount_ReturnsOwnershipScopedNotFoundProblem()
    {
        using var factory = new CarWebApplicationFactory();
        var accountAccessor = factory.Services.GetRequiredService<ICurrentAccountAccessor>();
        var accountId = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.ApplicationService
            .Setup(service => service.GetVehicleAsync(OtherUserVehicleId, It.IsAny<CancellationToken>()))
            .Returns((Guid _, CancellationToken _) =>
            {
                accountId.TrySetResult(accountAccessor.GetAccountId());
                return Task.FromException<VehicleDetailDto>(
                    new CarApplicationException(CarDomainErrorCodes.VehicleNotFound));
            });

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.GetAsync($"/api/V1/car/vehicles/{OtherUserVehicleId}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, CarDomainErrorCodes.VehicleNotFound);
        Assert.Equal(UserId, await accountId.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task ArchivedChildMutation_ReturnsConflictProblem()
    {
        using var factory = new CarWebApplicationFactory();
        factory.ApplicationService
            .Setup(service => service.CreateMaintenanceItemAsync(
                It.IsAny<CreateMaintenanceItemCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CarApplicationException(CarDomainErrorCodes.VehicleArchived));

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.PostAsJsonAsync(
            $"/api/V1/car/vehicles/{OtherUserVehicleId}/maintenance",
            new
            {
                name = "Oil",
                intervalMonths = 12,
            });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, CarDomainErrorCodes.VehicleArchived);
    }

    [Fact]
    public async Task VehicleUpdateConcurrencyFailure_ReturnsConflictProblem()
    {
        using var factory = new CarWebApplicationFactory();
        factory.ApplicationService
            .Setup(service => service.UpdateVehicleAsync(
                It.IsAny<UpdateVehicleCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CarApplicationException(CarDomainErrorCodes.ConcurrencyConflict));

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.PutAsJsonAsync(
            $"/api/V1/car/vehicles/{OtherUserVehicleId}",
            new
            {
                displayName = "Daily",
                make = "Make",
                model = "Model",
                year = 2026,
                plate = "ABC",
            });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, CarDomainErrorCodes.ConcurrencyConflict);
    }

    [Fact]
    public async Task Vehicles_WhenDatabaseUnavailable_Returns503Problem()
    {
        using var factory = new CarWebApplicationFactory();
        factory.ApplicationService
            .Setup(service => service.GetVehiclesAsync(It.IsAny<GetVehiclesQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CarApplicationException(CarDomainErrorCodes.DatabaseUnavailable));

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.GetAsync("/api/V1/car/vehicles");

        await AssertProblemAsync(response, HttpStatusCode.ServiceUnavailable, CarDomainErrorCodes.DatabaseUnavailable);
    }

    [Theory]
    [InlineData("page=bad&pageSize=25", "page")]
    [InlineData("page=0&pageSize=bad", "pageSize")]
    public async Task History_WhenPaginationQueryCannotBind_ReturnsStableValidationProblem(
        string query,
        string propertyName)
    {
        using var factory = new CarWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient(UserId);
        using var response = await client.GetAsync(
            $"/api/V1/car/vehicles/{OtherUserVehicleId}/history?{query}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            (int)HttpStatusCode.UnprocessableEntity,
            document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            CarApplicationErrorCodes.HistoryPaginationInvalid,
            document.RootElement.GetProperty("detail").GetString());
        Assert.Equal(
            CarApplicationErrorCodes.HistoryPaginationInvalid,
            document.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            CarApplicationErrorCodes.HistoryPaginationInvalid,
            document.RootElement.GetProperty("errors").GetProperty(propertyName)[0].GetString());
    }

    [Fact]
    public async Task Swagger_DeclaresProblemDetailsResponsesForCarOperations()
    {
        using var factory = new CarWebApplicationFactory();
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        var operations = paths
            .EnumerateObject()
            .Where(path => path.Name.StartsWith("/api/V1/car/", StringComparison.Ordinal))
            .SelectMany(path => path.Value.EnumerateObject())
            .Where(operation => operation.Name is "get" or "post" or "put" or "delete")
            .ToArray();

        Assert.Equal(18, operations.Length);
        foreach (var operation in operations)
        {
            var responses = operation.Value.GetProperty("responses");
            foreach (var status in new[] { "401", "403", "404", "409", "422", "503", "500" })
            {
                Assert.True(
                    responses.TryGetProperty(status, out var response),
                    $"Missing {status} for {operation.Name}.");
                Assert.True(response.TryGetProperty("content", out var content), $"Missing content for {status}.");
                Assert.Contains(
                    content.EnumerateObject(),
                    mediaType => mediaType.Value.TryGetProperty("schema", out var schema)
                        && schema.TryGetProperty("$ref", out var reference)
                        && reference.GetString()?.EndsWith("/ProblemDetails", StringComparison.Ordinal) == true);
            }
        }
    }

    [Fact]
    public async Task Vehicles_RequestCancellation_PropagatesToApplicationService()
    {
        using var factory = new CarWebApplicationFactory();
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.ApplicationService
            .Setup(service => service.GetVehiclesAsync(It.IsAny<GetVehiclesQuery>(), It.IsAny<CancellationToken>()))
            .Returns((GetVehiclesQuery _, CancellationToken cancellationToken) =>
                WaitForCancellationAsync(cancellationToken, started));

        using var client = factory.CreateAuthenticatedClient(UserId);
        using var cancellation = new CancellationTokenSource();
        var request = client.GetAsync("/api/V1/car/vehicles", cancellation.Token);
        var serverToken = await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        Assert.True(serverToken.IsCancellationRequested);
    }

    [Fact]
    public void WebApplicationFactory_PreservesExistingMongoEnvironmentVariable()
    {
        const string preservedConnectionString = "mongodb://preserved-host:27017";
        var previousConnectionString = Environment.GetEnvironmentVariable(
            CarWebApplicationFactory.MongoConnectionStringEnvironmentVariable,
            EnvironmentVariableTarget.Process);

        Environment.SetEnvironmentVariable(
            CarWebApplicationFactory.MongoConnectionStringEnvironmentVariable,
            preservedConnectionString,
            EnvironmentVariableTarget.Process);

        try
        {
            using (var factory = new CarWebApplicationFactory())
            using (factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }))
            {
            }

            Assert.Equal(
                preservedConnectionString,
                Environment.GetEnvironmentVariable(
                    CarWebApplicationFactory.MongoConnectionStringEnvironmentVariable,
                    EnvironmentVariableTarget.Process));
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                CarWebApplicationFactory.MongoConnectionStringEnvironmentVariable,
                previousConnectionString,
                EnvironmentVariableTarget.Process);
        }
    }

    private static async Task<VehiclePageDto> WaitForCancellationAsync(
        CancellationToken cancellationToken,
        TaskCompletionSource<CancellationToken> started)
    {
        started.TrySetResult(cancellationToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return new VehiclePageDto();
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var problem = document.RootElement;
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.Equal(expectedCode, problem.GetProperty("detail").GetString());
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }
}

internal sealed class CarWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string JwtKey = "Defender.CarService.Local.Development.Key.2026";
    private const string TestMongoConnectionString = "mongodb://localhost:27017";
    private static readonly object MongoEnvironmentLock = new();
    internal const string MongoConnectionStringEnvironmentVariable = "Defender_App_MongoDBConnectionString";

    public CarWebApplicationFactory()
    {
        ApplicationService = new Mock<IMyGarageApplicationService>(MockBehavior.Strict);
    }

    public Mock<IMyGarageApplicationService> ApplicationService { get; }

    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(userId));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Debug");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("JwtTokenIssuer", "Defender"),
                new KeyValuePair<string, string?>("JwtTokenAudience", "defender-api"),
                new KeyValuePair<string, string?>("JwtLocalDevelopmentKey", JwtKey),
            ]));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMyGarageApplicationService>();
            services.AddSingleton(ApplicationService.Object);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        lock (MongoEnvironmentLock)
        {
            var previousConnectionString = Environment.GetEnvironmentVariable(
                MongoConnectionStringEnvironmentVariable,
                EnvironmentVariableTarget.Process);

            try
            {
                Environment.SetEnvironmentVariable(
                    MongoConnectionStringEnvironmentVariable,
                    TestMongoConnectionString,
                    EnvironmentVariableTarget.Process);
                return base.CreateHost(builder);
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    MongoConnectionStringEnvironmentVariable,
                    previousConnectionString,
                    EnvironmentVariableTarget.Process);
            }
        }
    }

    private static string CreateToken(Guid userId)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "Defender",
            audience: "defender-api",
            claims:
            [
                new Claim(Defender.Common.Consts.ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(System.Security.Claims.ClaimTypes.Role, Roles.User),
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
