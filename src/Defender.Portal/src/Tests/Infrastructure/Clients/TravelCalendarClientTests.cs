using System.Net;
using System.Net.Http.Headers;
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

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}"),
            });
        }
    }
}
