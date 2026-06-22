const thirtyDaysMs = 30 * 24 * 60 * 60 * 1000;

export const healthCareShareGuideStorageKey =
  "healthCare.shareGuide.dismissedAt";

export const shouldShowHealthCareShareGuide = (now = Date.now()) => {
  const rawValue = localStorage.getItem(healthCareShareGuideStorageKey);

  if (!rawValue) {
    return true;
  }

  const dismissedAt = Date.parse(rawValue);

  if (Number.isNaN(dismissedAt)) {
    return true;
  }

  return now - dismissedAt >= thirtyDaysMs;
};

export const dismissHealthCareShareGuide = (now = Date.now()) => {
  localStorage.setItem(
    healthCareShareGuideStorageKey,
    new Date(now).toISOString()
  );
};
