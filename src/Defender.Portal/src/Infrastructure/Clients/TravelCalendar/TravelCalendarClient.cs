using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using Defender.Common.Exceptions;
using Defender.Common.Interfaces;
using Defender.Common.Wrapper.Internal;
using Defender.Portal.Application.Configuration.Options;
using Defender.Portal.Application.DTOs.TravelCalendar;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Defender.Portal.Infrastructure.Clients.TravelCalendar;

public class TravelCalendarClient(HttpClient httpClient, IAuthenticationHeaderAccessor authentication, IOptions<TravelCalendarOptions> options) : ITravelCalendarClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private string Url(string path = "") => $"{options.Value.Url.TrimEnd('/')}/api/V1/travel-calendar{path}";

    public async Task<TravelCalendarDto> GetAsync(string? from, string? to, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(from))
        {
            query["from"] = from;
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            query["to"] = to;
        }

        await Authorize(); var response = await httpClient.GetAsync(QueryHelpers.AddQueryString(Url(), query), ct); await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<TravelCalendarDto>(JsonOptions, ct))!;
    }

    public async Task<TravelCalendarMutationResultDto> SendAsync(HttpMethod method, string path, object request, CancellationToken ct = default)
    {
        await Authorize();
        using var message = new HttpRequestMessage(method, Url(path)) { Content = JsonContent.Create(request, options: JsonOptions) };
        var response = await httpClient.SendAsync(message, ct); await EnsureSuccess(response, ct);
        return (await response.Content.ReadFromJsonAsync<TravelCalendarMutationResultDto>(JsonOptions, ct))!;
    }

    private async Task Authorize() => httpClient.DefaultRequestHeaders.Authorization = await authentication.GetAuthenticationHeader(AuthorizationType.User);
    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new ApiException($"HTTP {(int)response.StatusCode}", (int)response.StatusCode, body, new ReadOnlyDictionary<string, IEnumerable<string>>(response.Headers.ToDictionary(item => item.Key, item => item.Value)), null!);
    }
}
