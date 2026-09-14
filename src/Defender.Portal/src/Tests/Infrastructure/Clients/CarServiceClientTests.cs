using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Defender.Common.Interfaces;
using Defender.Common.Wrapper.Internal;
using Defender.Portal.Application.Configuration.Options;
using Defender.Portal.Application.DTOs.MyGarage;
using Defender.Portal.Application.Enums;
using Defender.Portal.Application.Models.ApiRequests.MyGarage;
using Defender.Portal.Infrastructure.Clients.CarService;
using Microsoft.Extensions.Options;

namespace Defender.Portal.Tests.Infrastructure.Clients;

public sealed class CarServiceClientTests
{
    [Fact]
    public async Task GetVehiclesAsync_WhenCalled_SendsRouteQueryAndUserAuthorization()
    {
        var handler = new CapturingHandler(Json("[]"));
        var authentication = CreateAuthenticationMock();
        var sut = CreateClient(handler, authentication.Object);

        await sut.GetVehiclesAsync(true, CancellationToken.None);

        Assert.Equal("GET", handler.Request!.Method.Method);
        Assert.Equal("/api/V1/car/vehicles?includeArchived=true", handler.Request.RequestUri!.PathAndQuery);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("portal-token", handler.Request.Headers.Authorization.Parameter);
        authentication.Verify(item => item.GetAuthenticationHeader(AuthorizationType.User), Times.Once);
    }

    [Fact]
    public async Task CreateVehicleAsync_WhenCalled_SendsTypedJsonToBackendRoute()
    {
        var handler = new CapturingHandler(Json("{\"id\":\"00000000-0000-0000-0000-000000000001\",\"displayName\":\"Roadster\"}"));
        var sut = CreateClient(handler, CreateAuthenticationMock().Object);

        var result = await sut.CreateVehicleAsync(new CreateVehicleRequest
        {
            DisplayName = "Roadster",
            Make = "Example",
            Model = "X",
            Year = 2026,
            Plate = "ABC-123",
            Vin = null,
        });

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("/api/V1/car/vehicles", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("Roadster", result.DisplayName);
        Assert.Contains("\"displayName\":\"Roadster\"", handler.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("userId", handler.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetVehicleAsync_WhenCalled_UsesGuidRouteId()
    {
        var vehicleId = Guid.NewGuid();
        var handler = new CapturingHandler(Json("{\"vehicle\":{\"id\":\"" + vehicleId + "\"},\"maintenanceItems\":[],\"insurancePolicies\":[]}"));
        var sut = CreateClient(handler, CreateAuthenticationMock().Object);

        await sut.GetVehicleAsync(vehicleId, CancellationToken.None);

        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}", handler.Request!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateVehicleAsync_WhenCalled_UsesPutAndRouteId()
    {
        var vehicleId = Guid.NewGuid();
        var handler = new CapturingHandler(Json("{\"id\":\"" + vehicleId + "\"}"));
        var sut = CreateClient(handler, CreateAuthenticationMock().Object);

        await sut.UpdateVehicleAsync(vehicleId, new UpdateVehicleRequest(), CancellationToken.None);

        Assert.Equal(HttpMethod.Put, handler.Request!.Method);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}", handler.Request.RequestUri!.AbsolutePath);
    }

    [Theory]
    [InlineData("archive")]
    [InlineData("unarchive")]
    public async Task VehicleStateMutationAsync_WhenCalled_UsesPostSuffix(string suffix)
    {
        var vehicleId = Guid.NewGuid();
        var handler = new CapturingHandler(Json("{\"id\":\"" + vehicleId + "\"}"));
        var sut = CreateClient(handler, CreateAuthenticationMock().Object);

        if (suffix == "archive")
        {
            await sut.ArchiveVehicleAsync(vehicleId, CancellationToken.None);
        }
        else
        {
            await sut.UnarchiveVehicleAsync(vehicleId, CancellationToken.None);
        }

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}/{suffix}", handler.Request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task MaintenanceMethods_WhenCalled_UseVehicleAndMaintenanceIds()
    {
        var vehicleId = Guid.NewGuid();
        var maintenanceId = Guid.NewGuid();
        var handler = new CapturingHandler(Json("[]"));
        var sut = CreateClient(handler, CreateAuthenticationMock().Object);

        await sut.GetMaintenanceItemsAsync(vehicleId, CancellationToken.None);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}/maintenance", handler.Request!.RequestUri!.AbsolutePath);

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = Json("{\"id\":\"" + maintenanceId + "\"}") };
        await sut.CreateMaintenanceItemAsync(vehicleId, new CreateMaintenanceItemRequest(), CancellationToken.None);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = Json("{\"id\":\"" + maintenanceId + "\"}") };
        await sut.UpdateMaintenanceItemAsync(vehicleId, maintenanceId, new UpdateMaintenanceItemRequest(), CancellationToken.None);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}/maintenance/{maintenanceId}", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Put, handler.Request.Method);

        handler.Response = new HttpResponseMessage(HttpStatusCode.NoContent);
        await sut.DeleteMaintenanceItemAsync(vehicleId, maintenanceId, CancellationToken.None);
        Assert.Equal(HttpMethod.Delete, handler.Request.Method);
    }

    [Fact]
    public async Task HistoryMethods_WhenCalled_UsePaginationAndIds()
    {
        var vehicleId = Guid.NewGuid();
        var historyId = Guid.NewGuid();
        var handler = new CapturingHandler(Json("{\"items\":[],\"totalItemsCount\":0,\"currentPage\":2,\"pageSize\":10,\"totalPagesCount\":0}"));
        var sut = CreateClient(handler, CreateAuthenticationMock().Object);

        await sut.GetHistoryAsync(vehicleId, 2, 10, CancellationToken.None);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}/history?page=2&pageSize=10", handler.Request!.RequestUri!.PathAndQuery);

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = Json("{\"id\":\"" + historyId + "\"}") };
        await sut.CreateHistoryAsync(vehicleId, new CreateServiceHistoryRequest(), CancellationToken.None);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = Json("{\"id\":\"" + historyId + "\"}") };
        await sut.UpdateHistoryAsync(vehicleId, historyId, new UpdateServiceHistoryRequest(), CancellationToken.None);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}/history/{historyId}", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Put, handler.Request.Method);

        handler.Response = new HttpResponseMessage(HttpStatusCode.NoContent);
        await sut.DeleteHistoryAsync(vehicleId, historyId, CancellationToken.None);
        Assert.Equal(HttpMethod.Delete, handler.Request.Method);
    }

    [Fact]
    public async Task InsuranceMethods_WhenCalled_DoNotExposeDeleteAndUseRouteIds()
    {
        var vehicleId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var handler = new CapturingHandler(Json("[]"));
        var sut = CreateClient(handler, CreateAuthenticationMock().Object);

        await sut.GetInsurancePoliciesAsync(vehicleId, CancellationToken.None);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}/insurance", handler.Request!.RequestUri!.AbsolutePath);

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = Json("{\"id\":\"" + policyId + "\"}") };
        await sut.CreateInsurancePolicyAsync(vehicleId, new CreateInsurancePolicyRequest(), CancellationToken.None);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = Json("{\"id\":\"" + policyId + "\"}") };
        await sut.UpdateInsurancePolicyAsync(vehicleId, policyId, new UpdateInsurancePolicyRequest(), CancellationToken.None);
        Assert.Equal($"/api/V1/car/vehicles/{vehicleId}/insurance/{policyId}", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Put, handler.Request.Method);
        Assert.DoesNotContain("DeleteInsurance", typeof(ICarServiceClient).GetMethods().Select(item => item.Name));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "CAR_VEHICLE_NOT_FOUND")]
    [InlineData(HttpStatusCode.Conflict, "CAR_CONCURRENCY_CONFLICT")]
    [InlineData((HttpStatusCode)422, "CAR_HISTORY_COST_PAIR_INVALID")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "CAR_DATABASE_UNAVAILABLE")]
    [InlineData(HttpStatusCode.InternalServerError, "CAR_UNHANDLED_ERROR")]
    public async Task GetVehiclesAsync_WhenUpstreamFails_PreservesStatusCodeAndCarCode(HttpStatusCode status, string code)
    {
        var handler = new CapturingHandler(new HttpResponseMessage(status)
        {
            Content = Json("{\"code\":\"" + code + "\",\"detail\":\"upstream detail\"}"),
        });
        var sut = CreateClient(handler, CreateAuthenticationMock().Object);

        var exception = await Assert.ThrowsAsync<CarServiceUpstreamException>(() => sut.GetVehiclesAsync(false, CancellationToken.None));

        Assert.Equal((int)status, exception.Status);
        Assert.Equal(code, exception.Code);
        Assert.Equal("upstream detail", exception.Detail);
    }

    [Fact]
    public async Task GetVehiclesAsync_WhenErrorBodyRead_ConsumesBodyOnce()
    {
        var content = new CountingContent("{\"code\":\"CAR_UNHANDLED_ERROR\",\"detail\":\"failure\"}");
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = content });
        var sut = CreateClient(handler, CreateAuthenticationMock().Object);

        await Assert.ThrowsAsync<CarServiceUpstreamException>(() => sut.GetVehiclesAsync(false, CancellationToken.None));

        Assert.Equal(1, content.ReadCount);
    }

    private static Mock<IAuthenticationHeaderAccessor> CreateAuthenticationMock()
    {
        var authentication = new Mock<IAuthenticationHeaderAccessor>();
        authentication
            .Setup(item => item.GetAuthenticationHeader(AuthorizationType.User))
            .ReturnsAsync(new AuthenticationHeaderValue("Bearer", "portal-token"));
        return authentication;
    }

    private static CarServiceClient CreateClient(HttpMessageHandler handler, IAuthenticationHeaderAccessor authentication) =>
        new(new HttpClient(handler), authentication, Options.Create(new CarServiceOptions { Url = "https://car.test///" }));

    private static StringContent Json(string value) => new(value, Encoding.UTF8, "application/json");

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public CapturingHandler(HttpContent content)
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }

        public CapturingHandler(HttpResponseMessage response)
        {
            Response = response;
        }

        public HttpResponseMessage Response { get; set; }

        public HttpRequestMessage? Request { get; private set; }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return Response;
        }
    }

    private sealed class CountingContent(string value) : HttpContent
    {
        private readonly byte[] bytes = Encoding.UTF8.GetBytes(value);

        public int ReadCount { get; private set; }

        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new MemoryStream(bytes));

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            ReadCount++;
            return stream.WriteAsync(bytes).AsTask();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = bytes.Length;
            return true;
        }

    }
}
