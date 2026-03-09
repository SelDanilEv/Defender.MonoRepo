import apiUrls from "src/api/apiUrls";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import type IUtils from "src/appUtils/interface";

export interface PreferencesDto {
  userId: string;
  likes: string[];
  dislikes: string[];
}

export interface MenuSessionDto {
  id: string;
  userId: string;
  status: string;
  imageRefs: string[];
  parsedItems: string[];
  confirmedItems: string[];
  rankedItems: string[];
  trySomethingNew: boolean;
  recommendationWarningCode: string | null;
  recommendationWarningMessage: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface DishRatingDto {
  id: string;
  userId: string;
  dishName: string;
  rating: number;
  sessionId: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

const parseJsonSafe = async <T>(response: Response): Promise<T | null> => {
  const text = await response.text();
  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text) as T;
  } catch {
    return null;
  }
};

const withAuth = (options: RequestInit): RequestInit => {
  return {
    ...options,
    credentials: options.credentials ?? "same-origin",
  };
};

export const foodAdvisorApi = {
  getPreferences: (utils?: IUtils | null) =>
    new Promise<PreferencesDto | null>((resolve) => {
      APICallWrapper({
        url: apiUrls.foodAdvisor.getPreferences,
        options: { method: "GET" },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<PreferencesDto>(response);
          resolve(data);
        },
        onFailure: async () => resolve(null),
        showError: false,
      });
    }),

  updatePreferences: (
    likes: string[],
    dislikes: string[],
    utils?: IUtils | null
  ): Promise<PreferencesDto> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: apiUrls.foodAdvisor.updatePreferences,
        options: {
          method: "PUT",
          body: JSON.stringify({ likes, dislikes }),
        },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<PreferencesDto>(response);
          if (data) {
            resolve(data);
            return;
          }
          reject();
        },
        onFailure: async () => reject(),
        showSuccess: true,
        showError: true,
      });
    }),

  createSession: (utils?: IUtils | null): Promise<MenuSessionDto> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: apiUrls.foodAdvisor.createSession,
        options: { method: "POST" },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<MenuSessionDto>(response);
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

  getSessions: (utils?: IUtils | null): Promise<MenuSessionDto[]> =>
    new Promise((resolve) => {
      APICallWrapper({
        url: apiUrls.foodAdvisor.getSessions,
        options: { method: "GET" },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<MenuSessionDto[]>(response);
          resolve(Array.isArray(data) ? data : []);
        },
        onFailure: async () => resolve([]),
        showError: false,
      });
    }),

  getSession: (
    sessionId: string,
    utils?: IUtils | null
  ): Promise<MenuSessionDto | null> =>
    new Promise((resolve) => {
      APICallWrapper({
        url: `${apiUrls.foodAdvisor.getSession}/${sessionId}`,
        options: { method: "GET" },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<MenuSessionDto>(response);
          resolve(data);
        },
        onFailure: async () => resolve(null),
        showError: false,
      });
    }),

  deleteSession: (sessionId: string, utils?: IUtils | null): Promise<void> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: `${apiUrls.foodAdvisor.deleteSession}/${sessionId}`,
        options: { method: "DELETE" },
        utils,
        onSuccess: async () => resolve(),
        onFailure: async () => reject(),
        showSuccess: true,
        showError: true,
      });
    }),

  getRatings: (utils?: IUtils | null): Promise<DishRatingDto[]> =>
    new Promise((resolve) => {
      APICallWrapper({
        url: apiUrls.foodAdvisor.getRatings,
        options: { method: "GET" },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<DishRatingDto[]>(response);
          resolve(Array.isArray(data) ? data : []);
        },
        onFailure: async () => resolve([]),
        showError: false,
      });
    }),

  uploadSessionImages: (
    sessionId: string,
    files: File[],
    utils?: IUtils | null
  ): Promise<string[]> =>
    new Promise((resolve, reject) => {
      const formData = new FormData();
      files.forEach((f) => formData.append("files", f));
      const url = `${apiUrls.foodAdvisor.uploadImages}/${sessionId}/upload`;
      fetch(url, withAuth({ method: "POST", body: formData }))
        .then(async (res) => {
          if (res.ok) {
            const data = await parseJsonSafe<string[]>(res);
            resolve(Array.isArray(data) ? data : []);
          } else {
            const err = await parseJsonSafe<{ detail?: string }>(res);
            utils?.e?.(err?.detail);
            reject();
          }
        })
        .catch(reject);
    }),

  confirmMenu: (
    sessionId: string,
    confirmedItems: string[],
    trySomethingNew: boolean,
    utils?: IUtils | null
  ): Promise<MenuSessionDto> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: `${apiUrls.foodAdvisor.confirmMenu}/${sessionId}/confirm`,
        options: {
          method: "PATCH",
          body: JSON.stringify({ confirmedItems, trySomethingNew }),
        },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<MenuSessionDto>(response);
          if (data) {
            resolve(data);
            return;
          }
          reject();
        },
        onFailure: async () => reject(),
        showSuccess: true,
        showError: true,
      });
    }),

  requestParsing: (sessionId: string, utils?: IUtils | null): Promise<void> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: `${apiUrls.foodAdvisor.requestParsing}/${sessionId}/request-parsing`,
        options: { method: "POST" },
        utils,
        onSuccess: async () => resolve(),
        onFailure: async () => reject(),
        showError: true,
      });
    }),

  requestRecommendations: (
    sessionId: string,
    utils?: IUtils | null
  ): Promise<void> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: `${apiUrls.foodAdvisor.requestRecommendations}/${sessionId}/request-recommendations`,
        options: { method: "POST" },
        utils,
        onSuccess: async () => resolve(),
        onFailure: async () => reject(),
        showError: true,
      });
    }),

  getRecommendations: (
    sessionId: string,
    utils?: IUtils | null
  ): Promise<string[] | null> =>
    new Promise((resolve) => {
      APICallWrapper({
        url: `${apiUrls.foodAdvisor.getRecommendations}/${sessionId}/recommendations`,
        options: { method: "GET" },
        utils,
        onSuccess: async (response) => {
          const data = await parseJsonSafe<string[]>(response);
          resolve(Array.isArray(data) ? data : []);
        },
        onFailure: async () => resolve(null),
        showError: false,
      });
    }),

  submitRating: (
    dishName: string,
    rating: number,
    sessionId: string | null,
    utils?: IUtils | null
  ): Promise<void> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: apiUrls.foodAdvisor.submitRating,
        options: {
          method: "POST",
          body: JSON.stringify({
            dishName,
            rating,
            sessionId: sessionId || undefined,
          }),
        },
        utils,
        onSuccess: async () => resolve(),
        onFailure: async () => reject(),
        showSuccess: true,
        showError: true,
      });
    }),
};
