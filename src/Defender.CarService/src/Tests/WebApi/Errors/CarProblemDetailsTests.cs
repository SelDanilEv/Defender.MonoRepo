using Defender.CarService.Application.Common.Exceptions;
using Defender.CarService.Application.Common;
using Defender.CarService.WebApi.Errors;
using Defender.Common.Exceptions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Defender.CarService.Tests.WebApi.Errors;

public sealed class CarProblemDetailsTests
{
    [Theory]
    [InlineData("CAR_VEHICLE_NOT_FOUND", 404)]
    [InlineData("CAR_MAINTENANCE_REFERENCED", 409)]
    [InlineData("CAR_MAINTENANCE_BASELINE_LOCKED", 409)]
    [InlineData("CAR_ODOMETER_SEQUENCE_INVALID", 409)]
    [InlineData("CAR_CONCURRENCY_CONFLICT", 409)]
    [InlineData("CAR_DATABASE_UNAVAILABLE", 503)]
    [InlineData("CAR_HISTORY_DATE_FUTURE", 422)]
    public void ApplicationError_MapsStatusAndStableCode(string code, int status)
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-webapi" };

        var problem = CarProblemDetailsMapper.Map(context, new CarApplicationException(code));

        Assert.Equal(status, problem.Status);
        Assert.Equal(code, problem.Detail);
        Assert.Equal(code, problem.Extensions["code"]);
        Assert.Equal("trace-webapi", problem.Extensions["traceId"]);
    }

    [Fact]
    public void ValidationError_PreservesDetailCodeAndPropertyErrorArrays()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-validation" };
        var exception = new ValidationException(
            "CAR_HISTORY_COST_PAIR_INVALID",
            [new ValidationFailure("costAmountMinor", "CAR_HISTORY_COST_PAIR_INVALID")]);

        var problem = CarProblemDetailsMapper.Map(context, exception);

        Assert.Equal(422, problem.Status);
        Assert.Equal("CAR_HISTORY_COST_PAIR_INVALID", problem.Detail);
        Assert.Equal("CAR_HISTORY_COST_PAIR_INVALID", problem.Extensions["code"]);
        Assert.Equal("trace-validation", problem.Extensions["traceId"]);
        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.Extensions["errors"]);
        Assert.Equal("CAR_HISTORY_COST_PAIR_INVALID", errors["costAmountMinor"][0]);
    }

    [Fact]
    public void UnexpectedError_UsesStableUnhandledCode()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-unhandled" };

        var problem = CarProblemDetailsMapper.Map(context, new InvalidOperationException("secret"));

        Assert.Equal(500, problem.Status);
        Assert.Equal("CAR_UNHANDLED_ERROR", problem.Detail);
        Assert.Equal("CAR_UNHANDLED_ERROR", problem.Extensions["code"]);
        Assert.DoesNotContain("secret", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void ModelStateError_PreservesValidationCodeArray()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-model-state" };
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("displayName", "CAR_VEHICLE_DISPLAY_NAME_REQUIRED");
        modelState.AddModelError("displayName", "CAR_VEHICLE_FIELD_TOO_LONG");

        var problem = CarProblemDetailsMapper.FromModelState(context, modelState);

        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.Extensions["errors"]);
        Assert.Equal(
            ["CAR_VEHICLE_DISPLAY_NAME_REQUIRED", "CAR_VEHICLE_FIELD_TOO_LONG"],
            errors["displayName"]);
        Assert.Equal("CAR_VEHICLE_DISPLAY_NAME_REQUIRED", problem.Detail);
    }

    [Fact]
    public void ModelStateConversionError_UsesStablePropertyCode()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-model-conversion" };
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("type", "The JSON value could not be converted.");

        var problem = CarProblemDetailsMapper.FromModelState(context, modelState);

        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.Extensions["errors"]);
        Assert.Equal("CAR_HISTORY_TYPE_INVALID", errors["type"][0]);
        Assert.Equal("CAR_HISTORY_TYPE_INVALID", problem.Detail);
    }
}
