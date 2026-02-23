using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Defender.Common.Exceptions;
using Defender.Common.Interfaces;
using Defender.Common.Wrapper.Internal;
using Defender.Portal.Application.Configuration.Options;
using Defender.Portal.Application.DTOs.FoodAdviser;
using Microsoft.Extensions.Options;

namespace Defender.Portal.Infrastructure.Clients.PersonalFoodAdviser;

public class PersonalFoodAdviserClient(
    HttpClient httpClient,
    IAuthenticationHeaderAccessor authenticationHeaderAccessor,
    IOptions<PersonalFoodAdviserOptions> options) : IPersonalFoodAdviserClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private async Task SetAuthHeaderAsync()
    {
        var header = await authenticationHeaderAccessor.GetAuthenticationHeader(AuthorizationType.User);
        httpClient.DefaultRequestHeaders.Authorization = header;
    }

    private string BaseUrl => options.Value.Url.TrimEnd('/');

    private async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var headers = new Dictionary<string, IEnumerable<string>>();
        foreach (var h in response.Headers)
            headers[h.Key] = h.Value;
        throw new ApiException(
            $"HTTP {(int)response.StatusCode}",
            (int)response.StatusCode,
            responseBody ?? string.Empty,
            new ReadOnlyDictionary<string, IEnumerable<string>>(headers),
            null!);
    }

    public async Task<PortalPreferencesDto?> GetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.GetAsync($"{BaseUrl}/api/V1/Preferences", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PortalPreferencesDto>(JsonOptions, cancellationToken);
    }

    public async Task<PortalPreferencesDto> UpdatePreferencesAsync(IReadOnlyList<string> likes, IReadOnlyList<string> dislikes, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var body = JsonSerializer.Serialize(new { Likes = likes ?? [], Dislikes = dislikes ?? [] });
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PutAsync($"{BaseUrl}/api/V1/Preferences", content, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<PortalPreferencesDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<PortalMenuSessionDto> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.PostAsync($"{BaseUrl}/api/V1/MenuSession", null, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<PortalMenuSessionDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<PortalMenuSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.GetAsync($"{BaseUrl}/api/V1/MenuSession/{sessionId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PortalMenuSessionDto>(JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> UploadSessionImagesAsync(Guid sessionId, Stream[] fileStreams, string[] contentTypes, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        using var form = new MultipartFormDataContent();
        for (var i = 0; i < fileStreams.Length; i++)
        {
            var stream = fileStreams[i];
            var contentType = contentTypes?.Length > i ? contentTypes[i] : "image/jpeg";
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(streamContent, "files", $"image{i}.jpg");
        }
        var response = await httpClient.PostAsync($"{BaseUrl}/api/V1/MenuSession/{sessionId}/upload", form, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        var list = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions, cancellationToken);
        return list ?? [];
    }

    public async Task<PortalMenuSessionDto?> ConfirmMenuAsync(Guid sessionId, IReadOnlyList<string> confirmedItems, bool trySomethingNew, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var body = JsonSerializer.Serialize(new { ConfirmedItems = confirmedItems ?? [], TrySomethingNew = trySomethingNew });
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PatchAsync($"{BaseUrl}/api/V1/MenuSession/{sessionId}/confirm", content, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PortalMenuSessionDto>(JsonOptions, cancellationToken);
    }

    public async Task RequestParsingAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.PostAsync($"{BaseUrl}/api/V1/MenuSession/{sessionId}/request-parsing", null, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
    }

    public async Task RequestRecommendationsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.PostAsync($"{BaseUrl}/api/V1/MenuSession/{sessionId}/request-recommendations", null, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<string>?> GetRecommendationsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var response = await httpClient.GetAsync($"{BaseUrl}/api/V1/MenuSession/{sessionId}/recommendations", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions, cancellationToken);
    }

    public async Task SubmitRatingAsync(string dishName, int rating, Guid? sessionId, CancellationToken cancellationToken = default)
    {
        await SetAuthHeaderAsync();
        var body = JsonSerializer.Serialize(new { DishName = dishName, Rating = rating, SessionId = sessionId });
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"{BaseUrl}/api/V1/Rating", content, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
    }
}
