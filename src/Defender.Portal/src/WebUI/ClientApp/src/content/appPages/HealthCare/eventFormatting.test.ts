import type { HealthEvent } from "src/api/healthCare";
import {
  formatHealthEventType,
  formatHealthEventValue,
} from "./eventFormatting";

const t = (key: string, options?: Record<string, unknown>) => {
  if (key === "healthCare:medication_fallback") {
    return "Medication";
  }

  if (key === "healthCare:sleep_until") {
    return `Sleep until ${options?.time}`;
  }

  if (key === "healthCare:event_temperature") {
    return "Temperature";
  }

  if (key === "healthCare:event_medication") {
    return "Medication";
  }

  if (key === "healthCare:event_sleep") {
    return "Sleep";
  }

  if (key === "healthCare:event_wellbeing") {
    return "Wellbeing";
  }

  if (key === "healthCare:event_analysis") {
    return "Analysis";
  }

  if (key === "healthCare:analysis_status_bad") {
    return "Bad";
  }

  if (key === "healthCare:analysis_status_has_deviations") {
    return "Has deviations";
  }

  if (key === "healthCare:analysis_status_excellent") {
    return "Excellent";
  }

  return key;
};

describe("health care event formatting", () => {
  test("formatHealthEventValue_WhenAnalysisEvent_ReturnsNameAndStatus", () => {
    const event = {
      id: "1",
      type: "Analysis",
      startedAt: "2026-06-22T14:30:00Z",
      analysisName: "CRP",
      analysisStatus: "HasDeviations",
    } satisfies HealthEvent;

    const text = formatHealthEventValue(event, t, "en");

    expect(text).toBe("CRP Has deviations");
  });

  test("formatHealthEventValue_WhenSleepEventInRussian_UsesTwentyFourHourTime", () => {
    const event = {
      id: "2",
      type: "Sleep",
      startedAt: "2026-06-22T14:00:00Z",
      endedAt: "2026-06-22T14:30:00Z",
    } satisfies HealthEvent;

    const text = formatHealthEventValue(event, t, "ru");

    expect(text).toMatch(/\d{2}:\d{2}/);
    expect(text).not.toMatch(/AM|PM/i);
  });

  test("formatHealthEventType_WhenAnalysisEvent_ReturnsAnalysisLabel", () => {
    expect(formatHealthEventType("Analysis", t)).toBe("Analysis");
  });
});
