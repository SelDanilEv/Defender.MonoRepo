using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Common.Interfaces;
using Defender.HealthCareService.Application.Common.Interfaces.Repositories;
using Defender.HealthCareService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace WebApi.Controllers.V1;

public record MedicationOptionsResponse(
    IReadOnlyList<string> Names,
    IReadOnlyList<string> Amounts,
    IReadOnlyList<string> Units);

public class HealthEventsController(
    ICurrentAccountAccessor currentAccountAccessor,
    IHealthEventRepository healthEventRepository) : ControllerBase
{
    [HttpGet("api/health-events")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(IReadOnlyList<HealthEvent>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HealthEvent>>> GetEvents([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        var result = await healthEventRepository.GetHealthEventsAsync(currentAccountAccessor.GetAccountId(), from, to);

        return Ok(result);
    }

    [HttpGet("api/health-events/medication-options")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(MedicationOptionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MedicationOptionsResponse>> GetMedicationOptions()
    {
        var events = await healthEventRepository.GetHealthEventsAsync(currentAccountAccessor.GetAccountId(), null, null);
        var medicationEvents = events
            .Where(healthEvent => healthEvent.Type == HealthEventType.Medication)
            .ToArray();
        var response = new MedicationOptionsResponse(
            UniqueTextValues(medicationEvents.Select(healthEvent => healthEvent.MedicationName)),
            medicationEvents
                .Where(healthEvent => healthEvent.MedicationAmount != null)
                .Select(healthEvent => healthEvent.MedicationAmount!.Value)
                .Distinct()
                .OrderBy(amount => amount)
                .Select(amount => amount.ToString(CultureInfo.InvariantCulture))
                .ToArray(),
            UniqueTextValues(medicationEvents.Select(healthEvent => healthEvent.MedicationUnit)));

        return Ok(response);
    }

    [HttpPost("api/health-events")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(HealthEvent), StatusCodes.Status201Created)]
    public async Task<ActionResult<HealthEvent>> CreateEvent([FromBody] HealthEvent request)
    {
        var validationResult = ValidateHealthEvent(request);
        if (validationResult != null)
        {
            return validationResult;
        }

        request.Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
        request.UserId = currentAccountAccessor.GetAccountId();
        request.StartedAt = SnapToHalfHour(request.StartedAt);
        if (request.EndedAt != null)
        {
            request.EndedAt = SnapToHalfHour(request.EndedAt.Value);
        }

        await healthEventRepository.AddHealthEventAsync(request);
        return Created($"/api/health-events/{request.Id}", request);
    }

    [HttpPut("api/health-events/{id:guid}")]
    [Auth(Roles.User)]
    [ProducesResponseType(typeof(HealthEvent), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HealthEvent>> UpdateEvent(Guid id, [FromBody] HealthEvent request)
    {
        var validationResult = ValidateHealthEvent(request);
        if (validationResult != null)
        {
            return validationResult;
        }

        var userId = currentAccountAccessor.GetAccountId();
        var existing = await healthEventRepository.GetHealthEventByIdAsync(userId, id);

        if (existing == null)
        {
            return NotFound();
        }

        request.Id = id;
        request.UserId = userId;
        request.StartedAt = SnapToHalfHour(request.StartedAt);
        if (request.EndedAt != null)
        {
            request.EndedAt = SnapToHalfHour(request.EndedAt.Value);
        }

        return Ok(await healthEventRepository.UpdateHealthEventAsync(request));
    }

    [HttpDelete("api/health-events/{id:guid}")]
    [Auth(Roles.User)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var removed = await healthEventRepository.DeleteHealthEventAsync(currentAccountAccessor.GetAccountId(), id);
        return removed ? NoContent() : NotFound();
    }

    private BadRequestObjectResult? ValidateHealthEvent(HealthEvent request)
    {
        ClearFieldsForType(request);

        if (request.Type == HealthEventType.Wellbeing)
        {
            if (request.WellbeingScore is < 1 or > 5 or null)
            {
                return BadRequest("Wellbeing score must be between 1 and 5.");
            }

            return null;
        }

        if (request.Type == HealthEventType.Temperature)
        {
            if (request.TemperatureCelsius is null or < 36.4m or > 40.5m)
            {
                return BadRequest("Temperature must be between 36.4 and 40.5 Celsius.");
            }

            return null;
        }

        if (request.Type == HealthEventType.Analysis)
        {
            if (string.IsNullOrWhiteSpace(request.AnalysisName))
            {
                return BadRequest("Analysis name is required.");
            }

            if (request.AnalysisStatus == null)
            {
                return BadRequest("Analysis status is required.");
            }

            request.AnalysisName = request.AnalysisName.Trim();
        }

        return null;
    }

    private static void ClearFieldsForType(HealthEvent request)
    {
        if (request.Type != HealthEventType.Temperature)
        {
            request.TemperatureCelsius = null;
        }

        if (request.Type != HealthEventType.Wellbeing)
        {
            request.WellbeingScore = null;
        }

        if (request.Type != HealthEventType.Medication)
        {
            request.MedicationName = null;
            request.MedicationAmount = null;
            request.MedicationUnit = null;
        }

        if (request.Type != HealthEventType.Sleep)
        {
            request.EndedAt = null;
        }

        if (request.Type != HealthEventType.Analysis)
        {
            request.AnalysisName = null;
            request.AnalysisStatus = null;
        }
    }

    private static DateTimeOffset SnapToHalfHour(DateTimeOffset value)
    {
        var minutes = value.Minute < 30 ? 0 : 30;
        return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, minutes, 0, value.Offset);
    }

    private static IReadOnlyList<string> UniqueTextValues(IEnumerable<string?> values)
    {
        var options = new Dictionary<string, string>();

        foreach (var value in values)
        {
            var text = value?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            options.TryAdd(text.ToLowerInvariant(), text);
        }

        return options.Values
            .OrderBy(value => value, StringComparer.CurrentCulture)
            .ToArray();
    }
}
