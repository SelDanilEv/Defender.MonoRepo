using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using Defender.Common.Exceptions;
using Defender.Common.Interfaces;
using Defender.Common.Wrapper.Internal;
using Defender.Portal.Application.Configuration.Options;
using Defender.Portal.Application.DTOs.HealthCare;
using Defender.Portal.Application.Models.ApiRequests.HealthCare;
using Microsoft.Extensions.Options;

namespace Defender.Portal.Infrastructure.Clients.HealthCare;

public class HealthCareClient(
    HttpClient httpClient,
    IAuthenticationHeaderAccessor authenticationHeaderAccessor,
    IOptions<HealthCareOptions> options) : IHealthCareClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private string BaseUrl => options.Value.Url.TrimEnd('/');

    private async Task SetAuthHeaderAsync()
    {
        httpClient.DefaultRequestHeaders.Authorization =
            await authenticationHeaderAccessor.GetAuthenticationHeader(AuthorizationType.User);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var headers = new Dictionary<string, IEnumerable<string>>();
        foreach (var header in response.Headers)
        {
            headers[header.Key] = header.Value;
        }

        throw new ApiException(
            $"HTTP {(int)response.StatusCode}",
            (int)response.StatusCode,
            responseBody ?? string.Empty,
            new ReadOnlyDictionary<string, IEnumerable<string>>(headers),
            null!);
    }

    public async Task<IReadOnlyList<PortalHealthEventDto>> GetEventsAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var query = new List<string>();
        if (from != null) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to != null) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var url = $"{BaseUrl}/api/health-events{(query.Count > 0 ? "?" + string.Join("&", query) : string.Empty)}";
        var response = await httpClient.GetAsync(url, cancellationToken);

        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<PortalHealthEventDto>>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task<PortalHealthEventDto> CreateEventAsync(PortalHealthEventDto healthEvent, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/api/health-events", healthEvent, JsonOptions, cancellationToken);

        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<PortalHealthEventDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<PortalHealthEventDto?> UpdateEventAsync(Guid id, PortalHealthEventDto healthEvent, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/api/health-events/{id}", healthEvent, JsonOptions, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PortalHealthEventDto>(JsonOptions, cancellationToken);
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.DeleteAsync($"{BaseUrl}/api/health-events/{id}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return true;
    }

    public async Task<PortalHealthChartShareDto> CreateShareAsync(CreateHealthChartShareRequest request, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/api/health-chart-shares", request, JsonOptions, cancellationToken);

        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<PortalHealthChartShareDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<PortalHealthChartShareDto?> GetCurrentShareAsync(CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.GetAsync($"{BaseUrl}/api/health-chart-shares/current", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PortalHealthChartShareDto>(JsonOptions, cancellationToken);
    }

    public async Task<PortalHealthChartShareDto?> UpdateShareStatusAsync(UpdateHealthChartShareStatusRequest request, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/api/health-chart-shares/status", request, JsonOptions, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PortalHealthChartShareDto>(JsonOptions, cancellationToken);
    }

    public async Task<PortalHealthChartShareDto?> GetPublicShareAsync(string token, CancellationToken cancellationToken = default)
    {
        httpClient.DefaultRequestHeaders.Authorization = null;
        var response = await httpClient.GetAsync($"{BaseUrl}/api/public/health-chart-shares/{token}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PortalHealthChartShareDto>(JsonOptions, cancellationToken);
    }
}
