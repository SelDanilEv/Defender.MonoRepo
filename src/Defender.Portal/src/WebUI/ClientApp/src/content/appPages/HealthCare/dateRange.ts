import { HealthEvent } from "src/api/healthCare";

export type HealthDateRangePreset = "day" | "week" | "month" | "all";

export type HealthDateRangeSelection =
  | { kind: "preset"; preset: HealthDateRangePreset }
  | { kind: "custom"; from: Date; to: Date };

export interface DateRangeBounds {
  from?: Date;
  to?: Date;
}

const rangeDays: Partial<Record<HealthDateRangePreset, number>> = {
  day: 1,
  week: 7,
  month: 30,
};

const copyDate = (value: Date) => new Date(value.getTime());

const isValidDate = (value: Date | null): value is Date =>
  value !== null && !Number.isNaN(value.getTime());

export const resolveHealthDateRange = (
  selection: HealthDateRangeSelection,
  anchor = new Date()
): DateRangeBounds => {
  if (selection.kind === "custom") {
    return {
      from: copyDate(selection.from),
      to: copyDate(selection.to),
    };
  }

  const days = rangeDays[selection.preset];
  if (!days) {
    return { from: undefined, to: undefined };
  }

  const to = copyDate(anchor);
  const from = copyDate(anchor);
  from.setDate(from.getDate() - days);

  return { from, to };
};

export const validateHealthDateRange = (
  from: Date | null,
  to: Date | null
): string | null => {
  if (!isValidDate(from) || !isValidDate(to)) {
    return "Both dates are required.";
  }

  if (from.getTime() >= to.getTime()) {
    return "The start must be before the end.";
  }

  return null;
};

export const intersectDateRangeBounds = (
  bounds: DateRangeBounds,
  allowedBounds?: DateRangeBounds
): DateRangeBounds => {
  const from =
    bounds.from && allowedBounds?.from
      ? new Date(Math.max(bounds.from.getTime(), allowedBounds.from.getTime()))
      : copyDate(bounds.from ?? allowedBounds?.from ?? new Date(0));
  const to =
    bounds.to && allowedBounds?.to
      ? new Date(Math.min(bounds.to.getTime(), allowedBounds.to.getTime()))
      : bounds.to
        ? copyDate(bounds.to)
        : allowedBounds?.to
          ? copyDate(allowedBounds.to)
          : undefined;

  return {
    from:
      bounds.from || allowedBounds?.from
        ? from
        : undefined,
    to,
  };
};

export const filterEventsByDateRange = (
  events: HealthEvent[],
  selection: HealthDateRangeSelection,
  anchor = new Date(),
  allowedBounds?: DateRangeBounds
) => {
  const bounds = intersectDateRangeBounds(
    resolveHealthDateRange(selection, anchor),
    allowedBounds
  );

  if (bounds.from && bounds.to && bounds.from > bounds.to) {
    return [];
  }

  return events.filter((event) => {
    const startedAt = new Date(event.startedAt);

    return (
      !Number.isNaN(startedAt.getTime()) &&
      (!bounds.from || startedAt >= bounds.from) &&
      (!bounds.to || startedAt <= bounds.to)
    );
  });
};
