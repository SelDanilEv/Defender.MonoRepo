using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Defender.Common.Interfaces;
using Defender.Common.Wrapper.Internal;
using Defender.Portal.Application.Configuration.Options;
using Defender.Portal.Application.DTOs.HealthCare;
using Defender.Portal.Application.Models.ApiRequests.HealthCare;
using Defender.Portal.Infrastructure.Clients.HealthCare;
using Microsoft.Extensions.Options;

namespace Defender.Portal.Tests.Infrastructure.Clients;

public class HealthCareClientTests
{
    [Fact]
    public async Task CreateShareAsync_WhenRangeModeAndAnalysisAreReturned_PreservesContractMetadata()
    {
        var handler = new CapturingHandler(JsonResponse(HttpStatusCode.Created, """
            {
              "token": "stable-token",
              "publicUrl": "/api/public/health-chart-shares/stable-token",
              "events": [
                {
                  "id": "11111111-1111-1111-1111-111111111111",
                  "userId": "22222222-2222-2222-2222-222222222222",
                  "type": "Analysis",
                  "startedAt": "2026-06-18T08:30:00Z",
                  "analysisName": "Blood test",
                  "analysisStatus": "Excellent"
                }
              ],
              "from": "2026-06-18T08:30:00Z",
              "to": "2026-06-19T17:00:00Z",
              "rangeMode": "Absolute",
              "isEnabled": true,
              "createdAtUtc": "2026-06-18T08:30:00Z"
            }
            """));
        var sut = CreateClient(handler);
        var from = DateTimeOffset.Parse("2026-06-18T08:30:00Z");
        var to = DateTimeOffset.Parse("2026-06-19T17:00:00Z");

        var result = await sut.CreateShareAsync(
            new CreateHealthChartShareRequest(from, to, HealthChartShareRangeMode.Absolute));

        using var requestJson = JsonDocument.Parse(handler.Body!);
        Assert.Equal("Absolute", requestJson.RootElement.GetProperty("rangeMode").GetString());
        Assert.Equal(from, requestJson.RootElement.GetProperty("from").GetDateTimeOffset());
        Assert.Equal(to, requestJson.RootElement.GetProperty("to").GetDateTimeOffset());
        Assert.Equal(HealthChartShareRangeMode.Absolute, result.RangeMode);
        Assert.Single(result.Events);
        Assert.Equal(PortalHealthEventType.Analysis, result.Events[0].Type);
        Assert.Equal("Blood test", result.Events[0].AnalysisName);
        Assert.Equal(PortalAnalysisStatus.Excellent, result.Events[0].AnalysisStatus);
    }

    private static HealthCareClient CreateClient(CapturingHandler handler)
    {
        var authentication = new Mock<IAuthenticationHeaderAccessor>();
        authentication
            .Setup(item => item.GetAuthenticationHeader(AuthorizationType.User))
            .ReturnsAsync(new AuthenticationHeaderValue("Bearer", "token"));

        return new HealthCareClient(
            new HttpClient(handler),
            authentication.Object,
            Options.Create(new HealthCareOptions { Url = "https://health.test" }));
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
    };

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return response;
        }
    }
}
