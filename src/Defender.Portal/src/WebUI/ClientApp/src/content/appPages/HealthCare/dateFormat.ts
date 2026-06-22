const localesByLanguage: Record<string, string> = {
  en: "en-US",
  ru: "ru-RU",
};

const localeForLanguage = (language: string) =>
  localesByLanguage[language] ?? language;

const usesTwelveHourClock = (language: string) => language === "en";

export const formatEventTime = (value: Date, language: string) =>
  value.toLocaleTimeString(localeForLanguage(language), {
    hour: "2-digit",
    minute: "2-digit",
    hour12: usesTwelveHourClock(language),
  });

export const formatEventDateTime = (value: Date, language: string) =>
  value.toLocaleString(localeForLanguage(language), {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: usesTwelveHourClock(language),
  });
