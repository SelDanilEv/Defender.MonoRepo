using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Defender.Common.Interfaces;
using Defender.Common.Wrapper.Internal;
using Defender.Portal.Application.Configuration.Options;
using Defender.Portal.Application.DTOs.MyGarage;
using Defender.Portal.Application.Models.ApiRequests.MyGarage;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Defender.Portal.Infrastructure.Clients.CarService;

public sealed class CarServiceClient(
    HttpClient httpClient,
    IAuthenticationHeaderAccessor authenticationHeaderAccessor,
    IOptions<CarServiceOptions> options) : ICarServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    static CarServiceClient()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public Task<IReadOnlyList<VehicleSummaryDto>> GetVehiclesAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<VehicleSummaryDto>>(
            HttpMethod.Get,
            QueryHelpers.AddQueryString(
                "/vehicles",
                "includeArchived",
                includeArchived.ToString().ToLowerInvariant()),
            null,
            cancellationToken);

    public Task<VehicleDto> CreateVehicleAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<VehicleDto>(HttpMethod.Post, "/vehicles", request, cancellationToken);

    public Task<VehicleDetailDto> GetVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default) =>
        SendAsync<VehicleDetailDto>(HttpMethod.Get, $"/vehicles/{vehicleId}", null, cancellationToken);

    public Task<VehicleDto> UpdateVehicleAsync(
        Guid vehicleId,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<VehicleDto>(HttpMethod.Put, $"/vehicles/{vehicleId}", request, cancellationToken);

    public Task<VehicleDto> ArchiveVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default) =>
        SendAsync<VehicleDto>(HttpMethod.Post, $"/vehicles/{vehicleId}/archive", null, cancellationToken);

    public Task<VehicleDto> UnarchiveVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default) =>
        SendAsync<VehicleDto>(HttpMethod.Post, $"/vehicles/{vehicleId}/unarchive", null, cancellationToken);

    public Task<IReadOnlyList<MaintenanceItemDto>> GetMaintenanceItemsAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<MaintenanceItemDto>>(
            HttpMethod.Get,
            $"/vehicles/{vehicleId}/maintenance",
            null,
            cancellationToken);

    public Task<MaintenanceItemDto> CreateMaintenanceItemAsync(
        Guid vehicleId,
        CreateMaintenanceItemRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<MaintenanceItemDto>(
            HttpMethod.Post,
            $"/vehicles/{vehicleId}/maintenance",
            request,
            cancellationToken);

    public Task<MaintenanceItemDto> UpdateMaintenanceItemAsync(
        Guid vehicleId,
        Guid maintenanceId,
        UpdateMaintenanceItemRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<MaintenanceItemDto>(
            HttpMethod.Put,
            $"/vehicles/{vehicleId}/maintenance/{maintenanceId}",
            request,
            cancellationToken);

    public async Task DeleteMaintenanceItemAsync(
        Guid vehicleId,
        Guid maintenanceId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            $"/vehicles/{vehicleId}/maintenance/{maintenanceId}",
            null,
            cancellationToken);
    }

    public Task<ServiceHistoryPageDto> GetHistoryAsync(
        Guid vehicleId,
        int page = 0,
        int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        SendAsync<ServiceHistoryPageDto>(
            HttpMethod.Get,
            QueryHelpers.AddQueryString(
                $"/vehicles/{vehicleId}/history",
                new Dictionary<string, string?>
                {
                    ["page"] = page.ToString(),
                    ["pageSize"] = pageSize.ToString(),
                }),
            null,
            cancellationToken);

    public Task<ServiceHistoryRecordDto> CreateHistoryAsync(
        Guid vehicleId,
        CreateServiceHistoryRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<ServiceHistoryRecordDto>(
            HttpMethod.Post,
            $"/vehicles/{vehicleId}/history",
            request,
            cancellationToken);

    public Task<ServiceHistoryRecordDto> UpdateHistoryAsync(
        Guid vehicleId,
        Guid historyId,
        UpdateServiceHistoryRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<ServiceHistoryRecordDto>(
            HttpMethod.Put,
            $"/vehicles/{vehicleId}/history/{historyId}",
            request,
            cancellationToken);

    public async Task DeleteHistoryAsync(
        Guid vehicleId,
        Guid historyId,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            $"/vehicles/{vehicleId}/history/{historyId}",
            null,
            cancellationToken);
    }

    public Task<IReadOnlyList<InsurancePolicyDto>> GetInsurancePoliciesAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<InsurancePolicyDto>>(
            HttpMethod.Get,
            $"/vehicles/{vehicleId}/insurance",
            null,
            cancellationToken);

    public Task<InsurancePolicyDto> CreateInsurancePolicyAsync(
        Guid vehicleId,
        CreateInsurancePolicyRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<InsurancePolicyDto>(
            HttpMethod.Post,
            $"/vehicles/{vehicleId}/insurance",
            request,
            cancellationToken);

    public Task<InsurancePolicyDto> UpdateInsurancePolicyAsync(
        Guid vehicleId,
        Guid insuranceId,
        UpdateInsurancePolicyRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<InsurancePolicyDto>(
            HttpMethod.Put,
            $"/vehicles/{vehicleId}/insurance/{insuranceId}",
            request,
            cancellationToken);

    private string Url(string path) =>
        $"{options.Value.Url.TrimEnd('/')}/api/V1/car{path}";

    private async Task<TResult> SendAsync<TResult>(
        HttpMethod method,
        string path,
        object? requestBody,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(method, path, requestBody, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TResult>(JsonOptions, cancellationToken))!;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? requestBody,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, Url(path));
        request.Headers.Authorization = await authenticationHeaderAccessor.GetAuthenticationHeader(AuthorizationType.User);
        if (requestBody is not null)
        {
            request.Content = JsonContent.Create(requestBody, options: JsonOptions);
        }

        var response = await httpClient.SendAsync(request, cancellationToken);
        try
        {
            await EnsureSuccessAsync(response, cancellationToken);
        }
        catch
        {
            response.Dispose();
            throw;
        }

        return response;
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        string? code = null;
        var detail = response.StatusCode.ToString();

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            code = ReadString(root, "code") ?? ReadNestedExtension(root, "code");
            detail = ReadString(root, "detail") ?? detail;
        }
        catch (JsonException)
        {
        }

        throw new CarServiceUpstreamException((int)response.StatusCode, code, detail);
    }

    private static string? ReadNestedExtension(JsonElement root, string propertyName)
    {
        return root.TryGetProperty("extensions", out var extensions)
            ? ReadString(extensions, propertyName)
            : null;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
