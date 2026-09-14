using Defender.CarService.Application.Common;
using Defender.CarService.Application.Common.Exceptions;
using Defender.CarService.Domain.Exceptions;
using Defender.CarService.WebApi.Observability;
using Defender.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Defender.CarService.WebApi.Errors;

public static class CarProblemDetailsMapper
{
    private static readonly HashSet<string> NotFoundCodes =
    [
        CarDomainErrorCodes.VehicleNotFound,
        CarDomainErrorCodes.MaintenanceNotFound,
        CarDomainErrorCodes.HistoryNotFound,
        CarDomainErrorCodes.InsuranceNotFound,
    ];

    private static readonly HashSet<string> ConflictCodes =
    [
        CarDomainErrorCodes.VehicleArchived,
        CarDomainErrorCodes.MaintenanceReferenced,
        CarDomainErrorCodes.MaintenanceBaselineLocked,
        CarDomainErrorCodes.OdometerSequenceInvalid,
        CarDomainErrorCodes.ConcurrencyConflict,
    ];

    private static readonly HashSet<string> ValidationCodes =
    [
        CarApplicationErrorCodes.HistoryPaginationInvalid,
        CarDomainErrorCodes.VehicleDisplayNameRequired,
        CarDomainErrorCodes.VehicleFieldTooLong,
        CarDomainErrorCodes.VehicleYearInvalid,
        CarDomainErrorCodes.VehicleVinInvalid,
        CarDomainErrorCodes.MaintenanceNameRequired,
        CarDomainErrorCodes.MaintenanceIntervalRequired,
        CarDomainErrorCodes.MaintenanceIntervalInvalid,
        CarDomainErrorCodes.MaintenanceBaselineInvalid,
        CarDomainErrorCodes.HistoryDateInvalid,
        CarDomainErrorCodes.HistoryDateFuture,
        CarDomainErrorCodes.HistoryOdometerInvalid,
        CarDomainErrorCodes.HistoryTypeInvalid,
        CarDomainErrorCodes.HistoryTitleRequired,
        CarDomainErrorCodes.HistoryLinkInvalid,
        CarDomainErrorCodes.HistoryCostPairInvalid,
        CarDomainErrorCodes.HistoryCostInvalid,
        CarDomainErrorCodes.InsuranceProviderRequired,
        CarDomainErrorCodes.InsuranceDateRangeInvalid,
        CarDomainErrorCodes.InsuranceFieldTooLong,
        CarDomainErrorCodes.CurrencyInvalid,
    ];

    public static ProblemDetails Map(HttpContext context, Exception exception)
    {
        if (exception is ValidationException validationException)
        {
            return Create(
                context,
                StatusCodes.Status422UnprocessableEntity,
                validationException.Message,
                validationException.Errors.ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        if (exception is CarApplicationException applicationException)
        {
            return Create(
                context,
                GetStatusCode(applicationException.Code),
                applicationException.Code,
                null);
        }

        if (exception is CarDomainException domainException)
        {
            return Create(
                context,
                GetStatusCode(domainException.Code),
                domainException.Code,
                null);
        }

        if (exception is HttpRequestException)
        {
            return Create(
                context,
                StatusCodes.Status503ServiceUnavailable,
                CarDomainErrorCodes.DatabaseUnavailable,
                null);
        }

        if (exception is ForbiddenAccessException)
        {
            return Create(
                context,
                StatusCodes.Status403Forbidden,
                exception.Message,
                null);
        }

        if (exception is ServiceException serviceException)
        {
            return Create(
                context,
                StatusCodes.Status400BadRequest,
                serviceException.Message,
                null);
        }

        return Create(
            context,
            StatusCodes.Status500InternalServerError,
            CarDomainErrorCodes.UnhandledError,
            null);
    }

    public static ProblemDetails FromModelState(HttpContext context, ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(item => item.Value?.Errors.Count > 0)
            .ToDictionary(
                item => item.Key,
                item => item.Value!.Errors
                    .Select(error => GetModelStateCode(item.Key, error.ErrorMessage))
                    .ToArray());
        var code = errors.Values.SelectMany(values => values).FirstOrDefault()
            ?? CarDomainErrorCodes.UnhandledError;

        return Create(context, StatusCodes.Status422UnprocessableEntity, code, errors);
    }

    private static string GetModelStateCode(string propertyName, string? errorMessage = null)
    {
        if (!string.IsNullOrWhiteSpace(errorMessage)
            && errorMessage.StartsWith("CAR_", StringComparison.Ordinal))
        {
            return errorMessage;
        }

        var leafPropertyName = propertyName.Split('.').Last();
        return leafPropertyName.ToLowerInvariant() switch
        {
            "displayname" or "make" or "model" or "plate" => CarDomainErrorCodes.VehicleDisplayNameRequired,
            "year" => CarDomainErrorCodes.VehicleYearInvalid,
            "vin" => CarDomainErrorCodes.VehicleVinInvalid,
            "name" => CarDomainErrorCodes.MaintenanceNameRequired,
            "intervalmonths" or "intervalthousandkm" => CarDomainErrorCodes.MaintenanceIntervalInvalid,
            "lastdate" or "lastodometerkm" => CarDomainErrorCodes.MaintenanceBaselineInvalid,
            "type" => CarDomainErrorCodes.HistoryTypeInvalid,
            "date" => CarDomainErrorCodes.HistoryDateInvalid,
            "odometerkm" => CarDomainErrorCodes.HistoryOdometerInvalid,
            "title" or "notes" => CarDomainErrorCodes.HistoryTitleRequired,
            "linkedmaintenanceitemids" => CarDomainErrorCodes.HistoryLinkInvalid,
            "costcurrency" or "costamountminor" => CarDomainErrorCodes.HistoryCostInvalid,
            "provider" => CarDomainErrorCodes.InsuranceProviderRequired,
            "policynumber" or "coveragetype" => CarDomainErrorCodes.InsuranceFieldTooLong,
            "startdate" or "enddate" => CarDomainErrorCodes.InsuranceDateRangeInvalid,
            _ => CarDomainErrorCodes.UnhandledError,
        };
    }

    private static ProblemDetails Create(
        HttpContext context,
        int status,
        string code,
        IReadOnlyDictionary<string, string[]>? errors)
    {
        if (status == StatusCodes.Status422UnprocessableEntity)
        {
            CarServiceMetrics.ValidationFailures.Inc();
        }
        else
        {
            CarServiceMetrics.Record(code);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Detail = code,
            Title = "Car service request failed",
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        if (status == StatusCodes.Status422UnprocessableEntity && errors is null)
        {
            problem.Extensions["errors"] = new Dictionary<string, string[]>();
        }

        return problem;
    }

    private static int GetStatusCode(string code)
    {
        if (NotFoundCodes.Contains(code))
        {
            return StatusCodes.Status404NotFound;
        }

        if (ConflictCodes.Contains(code))
        {
            return StatusCodes.Status409Conflict;
        }

        if (ValidationCodes.Contains(code))
        {
            return StatusCodes.Status422UnprocessableEntity;
        }

        if (code == CarDomainErrorCodes.DatabaseUnavailable)
        {
            return StatusCodes.Status503ServiceUnavailable;
        }

        if (code == CarDomainErrorCodes.UnhandledError)
        {
            return StatusCodes.Status500InternalServerError;
        }

        return StatusCodes.Status400BadRequest;
    }
}
