import { describe, expect, test } from "vitest";

import { HistoryType, InsuranceStatus, MaintenanceStatus } from "src/models/myGarage/CarModels";

import {
  getInsuranceStatusColor,
  getGarageFailureMessage,
  getStatusColor,
  getStatusLabelKey,
} from "./helpers/status";

describe("My Garage status presentation", () => {
  test.each([
    [MaintenanceStatus.Overdue, "error"],
    [MaintenanceStatus.DueSoon, "warning"],
    [MaintenanceStatus.Upcoming, "success"],
    [MaintenanceStatus.NotStarted, "default"],
  ])("serverStatus_WhenReturnedAs%s_UsesSemanticColor", (status, color) => {
    expect(getStatusColor(status)).toBe(color);
    expect(getStatusLabelKey(status)).toBe(`statuses.${status}`);
  });

  test.each([
    [InsuranceStatus.Active, "success"],
    [InsuranceStatus.ExpiringSoon, "warning"],
    [InsuranceStatus.Expired, "default"],
  ])("insuranceStatus_WhenReturnedAs%s_UsesSemanticColor", (status, color) => {
    expect(getInsuranceStatusColor(status)).toBe(color);
    expect(getStatusLabelKey(status)).toBe(`statuses.${status}`);
  });

  test("statusPresentation_DoesNotRecalculateServerStatus", () => {
    expect(getStatusLabelKey(HistoryType.Repair)).toBe("types.Repair");
  });

  test("failureMessage_WhenCodeAndDetailDiffer_PrefersUpstreamCode", () => {
    const translate = (key: string) => ({
      "errors.CAR_CONCURRENCY_CONFLICT": "Conflict copy",
      "errors.generic": "Generic copy",
    }[key] ?? key);

    expect(getGarageFailureMessage({ code: "CAR_CONCURRENCY_CONFLICT", detail: "UnhandledError" }, translate)).toBe("Conflict copy");
    expect(getGarageFailureMessage({ status: 503 }, translate)).toBe("Generic copy");
  });
});
