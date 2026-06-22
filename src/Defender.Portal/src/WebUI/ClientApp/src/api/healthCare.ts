import apiUrls from "src/api/apiUrls";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import type IUtils from "src/appUtils/interface";

export type HealthEventType =
  | "Temperature"
  | "Medication"
  | "Sleep"
  | "Wellbeing"
  | "Analysis";

export type AnalysisStatus = "Bad" | "HasDeviations" | "Excellent";

export interface HealthEvent {
  id: string;
  userId?: string;
  type: HealthEventType;
  startedAt: string;
  endedAt?: string;
  temperatureCelsius?: number;
  medicationName?: string;
  medicationAmount?: number;
  medicationUnit?: string;
  wellbeingScore?: number;
  analysisName?: string;
  analysisStatus?: AnalysisStatus;
  notes?: string;
}

export interface HealthChartShare {
  token: string;
  publicUrl: string;
  events: HealthEvent[];
  from?: string;
  to?: string;
  isEnabled: boolean;
  createdAtUtc: string;
}

export interface MedicationOptions {
  names: string[];
  amounts: string[];
  units: string[];
}

export interface HealthChartShareRequest {
  from?: string;
  to?: string;
}

export interface HealthChartShareStatusRequest {
  isEnabled: boolean;
}

const parseJsonSafe = async <T>(response: Response): Promise<T | null> => {
  const text = await response.text();
  if (!text) return null;

  try {
    return JSON.parse(text) as T;
  } catch {
    return null;
  }
};

const normalizeEvents = (events: HealthEvent[]) =>
  [...events].sort(
    (left, right) =>
      new Date(right.startedAt).getTime() - new Date(left.startedAt).getTime()
  );

const withRangeQuery = (url: string, from?: string, to?: string) => {
  const params = new URLSearchParams();
  if (from) params.set("from", from);
  if (to) params.set("to", to);
  const query = params.toString();

  return query ? `${url}?${query}` : url;
};

export const healthCareApi = {
  getEvents: (
    from?: string,
    to?: string,
    utils?: IUtils | null
  ): Promise<HealthEvent[]> =>
    new Promise((resolve) => {
      APICallWrapper({
        url: withRangeQuery(apiUrls.healthCare.events, from, to),
        options: { method: "GET" },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<HealthEvent[]>(response);
          resolve(Array.isArray(data) ? normalizeEvents(data) : []);
        },
        onFailure: async () => resolve([]),
        showError: false,
      });
    }),

  getMedicationOptions: (utils?: IUtils | null): Promise<MedicationOptions> =>
    new Promise((resolve) => {
      APICallWrapper({
        url: apiUrls.healthCare.medicationOptions,
        options: { method: "GET" },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<MedicationOptions>(response);
          resolve(data ?? { names: [], amounts: [], units: [] });
        },
        onFailure: async () => resolve({ names: [], amounts: [], units: [] }),
        showError: false,
      });
    }),

  createEvent: (
    event: Omit<HealthEvent, "id">,
    utils?: IUtils | null
  ): Promise<HealthEvent> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: apiUrls.healthCare.events,
        options: {
          method: "POST",
          body: JSON.stringify(event),
        },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<HealthEvent>(response);
          if (data) {
            resolve(data);
            return;
          }
          reject();
        },
        onFailure: async () => reject(),
        showError: true,
      });
    }),

  updateEvent: (
    event: HealthEvent,
    utils?: IUtils | null
  ): Promise<HealthEvent> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: `${apiUrls.healthCare.events}/${event.id}`,
        options: {
          method: "PUT",
          body: JSON.stringify(event),
        },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<HealthEvent>(response);
          if (data) {
            resolve(data);
            return;
          }
          reject();
        },
        onFailure: async () => reject(),
        showError: true,
      });
    }),

  deleteEvent: (id: string, utils?: IUtils | null): Promise<void> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: `${apiUrls.healthCare.events}/${id}`,
        options: { method: "DELETE" },
        utils,
        onSuccess: async () => resolve(),
        onFailure: async () => reject(),
        showError: true,
      });
    }),

  createShare: (
    request: HealthChartShareRequest,
    utils?: IUtils | null
  ): Promise<HealthChartShare> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: apiUrls.healthCare.chartShares,
        options: {
          method: "POST",
          body: JSON.stringify(request),
        },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<HealthChartShare>(response);
          if (data) {
            resolve(data);
            return;
          }
          reject();
        },
        onFailure: async () => reject(),
        showError: true,
      });
    }),

  getCurrentShare: (utils?: IUtils | null): Promise<HealthChartShare | null> =>
    new Promise((resolve) => {
      APICallWrapper({
        url: `${apiUrls.healthCare.chartShares}/current`,
        options: { method: "GET" },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<HealthChartShare>(response);
          resolve(data);
        },
        onFailure: async () => resolve(null),
        showError: false,
      });
    }),

  updateShareStatus: (
    request: HealthChartShareStatusRequest,
    utils?: IUtils | null
  ): Promise<HealthChartShare | null> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: `${apiUrls.healthCare.chartShares}/status`,
        options: {
          method: "PUT",
          body: JSON.stringify(request),
        },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<HealthChartShare>(response);
          resolve(data);
        },
        onFailure: async () => reject(),
        showError: true,
      });
    }),

  getPublicShare: (
    token: string,
    utils?: IUtils | null
  ): Promise<HealthChartShare | null> =>
    new Promise((resolve) => {
      APICallWrapper({
        url: `${apiUrls.healthCare.publicChartShares}/${token}`,
        options: { method: "GET" },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<HealthChartShare>(response);
          resolve(data);
        },
        onFailure: async () => resolve(null),
        showError: false,
        doLock: false,
      });
    }),
};
