export type HealthEventType = "Temperature" | "Medication" | "Sleep";

export interface HealthEvent {
  id: string;
  type: HealthEventType;
  startedAt: string;
  endedAt?: string;
  temperatureCelsius?: number;
  medicationName?: string;
  medicationAmount?: number;
  medicationUnit?: string;
  notes?: string;
}

const storageKey = "defender.healthCare.events";

const load = (): HealthEvent[] => {
  try {
    return JSON.parse(localStorage.getItem(storageKey) || "[]");
  } catch {
    return [];
  }
};

const save = (events: HealthEvent[]) => {
  localStorage.setItem(storageKey, JSON.stringify(events));
};

const snapToHalfHour = (value: string) => {
  const date = new Date(value);
  date.setMinutes(date.getMinutes() < 30 ? 0 : 30, 0, 0);
  return date.toISOString();
};

export const healthCareApi = {
  getEvents: async () => load().sort((a, b) => a.startedAt.localeCompare(b.startedAt)),
  createEvent: async (event: Omit<HealthEvent, "id">) => {
    const events = load();
    const item = {
      ...event,
      id: (crypto as any).randomUUID ? (crypto as any).randomUUID() : `${Date.now()}-${Math.random()}`,
      startedAt: snapToHalfHour(event.startedAt),
      endedAt: event.endedAt ? snapToHalfHour(event.endedAt) : undefined,
    };
    save([...events, item]);
    return item;
  },
  deleteEvent: async (id: string) => save(load().filter((event) => event.id !== id)),
};
