namespace Defender.CarService.Domain.Exceptions;

public class CarDomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;

    public const string VehicleNotFound = CarDomainErrorCodes.VehicleNotFound;
    public const string VehicleArchived = CarDomainErrorCodes.VehicleArchived;
    public const string MaintenanceNotFound = CarDomainErrorCodes.MaintenanceNotFound;
    public const string MaintenanceReferenced = CarDomainErrorCodes.MaintenanceReferenced;
    public const string VehicleDisplayNameRequired = CarDomainErrorCodes.VehicleDisplayNameRequired;
    public const string VehicleFieldTooLong = CarDomainErrorCodes.VehicleFieldTooLong;
    public const string VehicleYearInvalid = CarDomainErrorCodes.VehicleYearInvalid;
    public const string VehicleVinInvalid = CarDomainErrorCodes.VehicleVinInvalid;
    public const string MaintenanceNameRequired = CarDomainErrorCodes.MaintenanceNameRequired;
    public const string MaintenanceIntervalRequired = CarDomainErrorCodes.MaintenanceIntervalRequired;
    public const string MaintenanceIntervalInvalid = CarDomainErrorCodes.MaintenanceIntervalInvalid;
    public const string MaintenanceBaselineInvalid = CarDomainErrorCodes.MaintenanceBaselineInvalid;
    public const string MaintenanceBaselineLocked = CarDomainErrorCodes.MaintenanceBaselineLocked;
    public const string HistoryDateInvalid = CarDomainErrorCodes.HistoryDateInvalid;
    public const string HistoryDateFuture = CarDomainErrorCodes.HistoryDateFuture;
    public const string HistoryOdometerInvalid = CarDomainErrorCodes.HistoryOdometerInvalid;
    public const string HistoryTypeInvalid = CarDomainErrorCodes.HistoryTypeInvalid;
    public const string HistoryTitleRequired = CarDomainErrorCodes.HistoryTitleRequired;
    public const string HistoryLinkInvalid = CarDomainErrorCodes.HistoryLinkInvalid;
    public const string HistoryCostPairInvalid = CarDomainErrorCodes.HistoryCostPairInvalid;
    public const string HistoryCostInvalid = CarDomainErrorCodes.HistoryCostInvalid;
    public const string InsuranceProviderRequired = CarDomainErrorCodes.InsuranceProviderRequired;
    public const string InsuranceDateRangeInvalid = CarDomainErrorCodes.InsuranceDateRangeInvalid;
    public const string InsuranceFieldTooLong = CarDomainErrorCodes.InsuranceFieldTooLong;
    public const string CurrencyInvalid = CarDomainErrorCodes.CurrencyInvalid;
    public const string OdometerSequenceInvalid = CarDomainErrorCodes.OdometerSequenceInvalid;
    public const string HistoryNotFound = CarDomainErrorCodes.HistoryNotFound;
    public const string InsuranceNotFound = CarDomainErrorCodes.InsuranceNotFound;
    public const string ConcurrencyConflict = CarDomainErrorCodes.ConcurrencyConflict;
    public const string DatabaseUnavailable = CarDomainErrorCodes.DatabaseUnavailable;
    public const string UnhandledError = CarDomainErrorCodes.UnhandledError;
}

public static class CarDomainErrorCodes
{
    public const string VehicleNotFound = "CAR_VEHICLE_NOT_FOUND";
    public const string VehicleArchived = "CAR_VEHICLE_ARCHIVED";
    public const string MaintenanceNotFound = "CAR_MAINTENANCE_NOT_FOUND";
    public const string MaintenanceReferenced = "CAR_MAINTENANCE_REFERENCED";
    public const string VehicleDisplayNameRequired = "CAR_VEHICLE_DISPLAY_NAME_REQUIRED";
    public const string VehicleFieldTooLong = "CAR_VEHICLE_FIELD_TOO_LONG";
    public const string VehicleYearInvalid = "CAR_VEHICLE_YEAR_INVALID";
    public const string VehicleVinInvalid = "CAR_VEHICLE_VIN_INVALID";
    public const string MaintenanceNameRequired = "CAR_MAINTENANCE_NAME_REQUIRED";
    public const string MaintenanceIntervalRequired = "CAR_MAINTENANCE_INTERVAL_REQUIRED";
    public const string MaintenanceIntervalInvalid = "CAR_MAINTENANCE_INTERVAL_INVALID";
    public const string MaintenanceBaselineInvalid = "CAR_MAINTENANCE_BASELINE_INVALID";
    public const string MaintenanceBaselineLocked = "CAR_MAINTENANCE_BASELINE_LOCKED";
    public const string HistoryDateInvalid = "CAR_HISTORY_DATE_INVALID";
    public const string HistoryDateFuture = "CAR_HISTORY_DATE_FUTURE";
    public const string HistoryOdometerInvalid = "CAR_HISTORY_ODOMETER_INVALID";
    public const string HistoryTypeInvalid = "CAR_HISTORY_TYPE_INVALID";
    public const string HistoryTitleRequired = "CAR_HISTORY_TITLE_REQUIRED";
    public const string HistoryLinkInvalid = "CAR_HISTORY_LINK_INVALID";
    public const string HistoryCostPairInvalid = "CAR_HISTORY_COST_PAIR_INVALID";
    public const string HistoryCostInvalid = "CAR_HISTORY_COST_INVALID";
    public const string InsuranceProviderRequired = "CAR_INSURANCE_PROVIDER_REQUIRED";
    public const string InsuranceDateRangeInvalid = "CAR_INSURANCE_DATE_RANGE_INVALID";
    public const string InsuranceFieldTooLong = "CAR_INSURANCE_FIELD_TOO_LONG";
    public const string CurrencyInvalid = "CAR_CURRENCY_INVALID";
    public const string OdometerSequenceInvalid = "CAR_ODOMETER_SEQUENCE_INVALID";
    public const string HistoryNotFound = "CAR_HISTORY_NOT_FOUND";
    public const string InsuranceNotFound = "CAR_INSURANCE_NOT_FOUND";
    public const string ConcurrencyConflict = "CAR_CONCURRENCY_CONFLICT";
    public const string DatabaseUnavailable = "CAR_DATABASE_UNAVAILABLE";
    public const string UnhandledError = "CAR_UNHANDLED_ERROR";
}

public static class CarDomainExceptionCodes
{
    public const string VehicleNotFound = CarDomainErrorCodes.VehicleNotFound;
    public const string VehicleArchived = CarDomainErrorCodes.VehicleArchived;
    public const string MaintenanceNotFound = CarDomainErrorCodes.MaintenanceNotFound;
    public const string MaintenanceReferenced = CarDomainErrorCodes.MaintenanceReferenced;
    public const string VehicleDisplayNameRequired = CarDomainErrorCodes.VehicleDisplayNameRequired;
    public const string VehicleFieldTooLong = CarDomainErrorCodes.VehicleFieldTooLong;
    public const string VehicleYearInvalid = CarDomainErrorCodes.VehicleYearInvalid;
    public const string VehicleVinInvalid = CarDomainErrorCodes.VehicleVinInvalid;
    public const string MaintenanceNameRequired = CarDomainErrorCodes.MaintenanceNameRequired;
    public const string MaintenanceIntervalRequired = CarDomainErrorCodes.MaintenanceIntervalRequired;
    public const string MaintenanceIntervalInvalid = CarDomainErrorCodes.MaintenanceIntervalInvalid;
    public const string MaintenanceBaselineInvalid = CarDomainErrorCodes.MaintenanceBaselineInvalid;
    public const string MaintenanceBaselineLocked = CarDomainErrorCodes.MaintenanceBaselineLocked;
    public const string HistoryDateInvalid = CarDomainErrorCodes.HistoryDateInvalid;
    public const string HistoryDateFuture = CarDomainErrorCodes.HistoryDateFuture;
    public const string HistoryOdometerInvalid = CarDomainErrorCodes.HistoryOdometerInvalid;
    public const string HistoryTypeInvalid = CarDomainErrorCodes.HistoryTypeInvalid;
    public const string HistoryTitleRequired = CarDomainErrorCodes.HistoryTitleRequired;
    public const string HistoryLinkInvalid = CarDomainErrorCodes.HistoryLinkInvalid;
    public const string HistoryCostPairInvalid = CarDomainErrorCodes.HistoryCostPairInvalid;
    public const string HistoryCostInvalid = CarDomainErrorCodes.HistoryCostInvalid;
    public const string InsuranceProviderRequired = CarDomainErrorCodes.InsuranceProviderRequired;
    public const string InsuranceDateRangeInvalid = CarDomainErrorCodes.InsuranceDateRangeInvalid;
    public const string InsuranceFieldTooLong = CarDomainErrorCodes.InsuranceFieldTooLong;
    public const string CurrencyInvalid = CarDomainErrorCodes.CurrencyInvalid;
    public const string OdometerSequenceInvalid = CarDomainErrorCodes.OdometerSequenceInvalid;
    public const string HistoryNotFound = CarDomainErrorCodes.HistoryNotFound;
    public const string InsuranceNotFound = CarDomainErrorCodes.InsuranceNotFound;
    public const string ConcurrencyConflict = CarDomainErrorCodes.ConcurrencyConflict;
    public const string DatabaseUnavailable = CarDomainErrorCodes.DatabaseUnavailable;
    public const string UnhandledError = CarDomainErrorCodes.UnhandledError;
}
