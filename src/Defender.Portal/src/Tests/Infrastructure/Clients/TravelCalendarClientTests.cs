using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Defender.Common.Interfaces;
using Defender.Common.Wrapper.Internal;
using Defender.Portal.Application.Configuration.Options;
using Defender.Portal.Infrastructure.Clients.TravelCalendar;
using Microsoft.Extensions.Options;

namespace Defender.Portal.Tests.Infrastructure.Clients;

public class TravelCalendarClientTests
{
    [Fact]
    public async Task GetAsync_WhenRangeProvided_SendsBothQueryParameters()
    {
        var handler = new CapturingHandler();
        var authentication = new Mock<IAuthenticationHeaderAccessor>();
        authentication
            .Setup(item => item.GetAuthenticationHeader(AuthorizationType.User))
            .ReturnsAsync(new AuthenticationHeaderValue("Bearer", "token"));
        var sut = new TravelCalendarClient(
            new HttpClient(handler),
            authentication.Object,
            Options.Create(new TravelCalendarOptions { Url = "https://calendar.test" }));

        await sut.GetAsync("2026-07-01", "2026-07-31");

        Assert.Equal("/api/V1/travel-calendar?from=2026-07-01&to=2026-07-31", handler.RequestUri!.PathAndQuery);
    }

    // Contract guard for the invitations fetch-unbounded fix: useTravelCalendar.ts's
    // load() now calls the Portal BFF with no from/to at all. This asserts the
    // already-existing client behavior (no production code changed here) really omits
    // both query parameters rather than sending them blank, which is what lets the BFF
    // cache key collapse to the canonical "{userId}_all_all" (see
    // TravelCalendarControllerTests.Get_WhenRangeOmitted_UsesTheCanonicalAllAllCacheKey).
    [Fact]
    public async Task GetAsync_WhenRangeOmitted_SendsNoQueryParameters()
    {
        var handler = new CapturingHandler();
        var authentication = new Mock<IAuthenticationHeaderAccessor>();
        authentication
            .Setup(item => item.GetAuthenticationHeader(AuthorizationType.User))
            .ReturnsAsync(new AuthenticationHeaderValue("Bearer", "token"));
        var sut = new TravelCalendarClient(
            new HttpClient(handler),
            authentication.Object,
            Options.Create(new TravelCalendarOptions { Url = "https://calendar.test" }));

        await sut.GetAsync(null, null);

        Assert.Equal("/api/V1/travel-calendar", handler.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task GetAsync_WhenUpstreamReturnsProblemDetails_PreservesStatusCodeAndErrorCode()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                "{\"detail\":\"These dates overlap\",\"code\":\"TRAVEL_CALENDAR_DATE_OVERLAP\"}",
                Encoding.UTF8,
                "application/problem+json"),
        });
        var authentication = new Mock<IAuthenticationHeaderAccessor>();
        authentication
            .Setup(item => item.GetAuthenticationHeader(AuthorizationType.User))
            .ReturnsAsync(new AuthenticationHeaderValue("Bearer", "token"));
        var sut = new TravelCalendarClient(
            new HttpClient(handler),
            authentication.Object,
            Options.Create(new TravelCalendarOptions { Url = "https://calendar.test" }));

        var exception = await Assert.ThrowsAsync<TravelCalendarUpstreamException>(() => sut.GetAsync(null, null));

        Assert.Equal(409, exception.Status);
        Assert.Equal("TRAVEL_CALENDAR_DATE_OVERLAP", exception.Code);
        Assert.Equal("These dates overlap", exception.Message);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage response;

        public CapturingHandler(HttpResponseMessage? response = null)
        {
            this.response = response ?? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}"),
            };
        }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(response);
        }
    }
}
