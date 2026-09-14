using Prometheus;
using Defender.CarService.Domain.Exceptions;

namespace Defender.CarService.WebApi.Observability;

public static class CarServiceMetrics
{
    public static readonly Counter ValidationFailures = Metrics.CreateCounter(
        "car_service_validation_failures",
        "Number of CarService request validation failures.");

    public static readonly Counter OwnershipNotFound = Metrics.CreateCounter(
        "car_service_ownership_not_found",
        "Number of ownership-scoped not-found results.");

    public static readonly Counter ConcurrencyConflicts = Metrics.CreateCounter(
        "car_service_concurrency_conflicts",
        "Number of optimistic concurrency conflicts.");

    public static readonly Counter TransactionFailures = Metrics.CreateCounter(
        "car_service_transaction_failures",
        "Number of transaction or database failures.");

    public static readonly Counter UpstreamPortalFailures = Metrics.CreateCounter(
        "car_service_upstream_portal_failures",
        "Number of upstream Portal failures.");

    public static void Initialize()
    {
    }

    public static void Record(string code)
    {
        switch (code)
        {
            case CarDomainErrorCodes.VehicleNotFound:
            case CarDomainErrorCodes.MaintenanceNotFound:
            case CarDomainErrorCodes.HistoryNotFound:
            case CarDomainErrorCodes.InsuranceNotFound:
                OwnershipNotFound.Inc();
                break;
            case CarDomainErrorCodes.ConcurrencyConflict:
                ConcurrencyConflicts.Inc();
                break;
            case CarDomainErrorCodes.DatabaseUnavailable:
                TransactionFailures.Inc();
                break;
        }
    }
}
