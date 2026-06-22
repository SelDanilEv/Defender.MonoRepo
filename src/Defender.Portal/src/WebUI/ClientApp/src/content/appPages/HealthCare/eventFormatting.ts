import { AnalysisStatus, HealthEvent, HealthEventType } from "src/api/healthCare";
import { wellbeingScoreToEmoji } from "./chartData";
import { formatEventTime } from "./dateFormat";

const medicationLabel = (
  event: HealthEvent,
  t: (key: string, options?: object) => string
) =>
  [
    event.medicationName || t("healthCare:medication_fallback"),
    event.medicationAmount,
    event.medicationUnit,
  ]
    .filter((value) => value !== undefined && value !== null && value !== "")
    .join(" ");

const localizedAnalysisStatus = (
  status: AnalysisStatus | undefined,
  t: (key: string, options?: object) => string
) => {
  const key = getAnalysisStatusTranslationKey(status);
  return key ? t(key) : "";
};

export const formatHealthEventValue = (
  event: HealthEvent,
  t: (key: string, options?: object) => string,
  language: string
) => {
  if (event.type === "Temperature") {
    return event.temperatureCelsius === undefined || event.temperatureCelsius === null
      ? "-"
      : `${event.temperatureCelsius.toFixed(1)} \u00b0C`;
  }

  if (event.type === "Medication") {
    return medicationLabel(event, t);
  }

  if (event.type === "Wellbeing") {
    return `${wellbeingScoreToEmoji(event.wellbeingScore)} ${event.wellbeingScore || ""}/5`;
  }

  if (event.type === "Analysis") {
    return [event.analysisName, localizedAnalysisStatus(event.analysisStatus, t)]
      .filter((value) => value)
      .join(" ");
  }

  const time = formatEventTime(new Date(event.endedAt || event.startedAt), language);
  return t("healthCare:sleep_until", { time });
};

export const formatHealthEventType = (
  eventType: HealthEventType,
  t: (key: string, options?: object) => string
) => {
  if (eventType === "Temperature") return t("healthCare:event_temperature");
  if (eventType === "Medication") return t("healthCare:event_medication");
  if (eventType === "Wellbeing") return t("healthCare:event_wellbeing");
  if (eventType === "Analysis") return t("healthCare:event_analysis");
  return t("healthCare:event_sleep");
};

export const analysisStatusOptions: AnalysisStatus[] = [
  "Bad",
  "HasDeviations",
  "Excellent",
];

export const getAnalysisStatusTranslationKey = (
  status: AnalysisStatus | undefined
) => {
  if (status === "Bad") {
    return "healthCare:analysis_status_bad";
  }

  if (status === "HasDeviations") {
    return "healthCare:analysis_status_has_deviations";
  }

  if (status === "Excellent") {
    return "healthCare:analysis_status_excellent";
  }

  return "";
};
