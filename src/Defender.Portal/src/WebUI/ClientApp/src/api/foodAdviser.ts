import apiUrls from "src/api/apiUrls";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import store from "src/state/store";

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
  const session = store.getState().session;
  const headers = new Headers(options.headers as HeadersInit);
  if (session.isAuthenticated && session.token) {
    headers.set("Authorization", `Bearer ${session.token}`);
  }
  return { ...options, headers };
};

export const foodAdviserApi = {
  getPreferences: (utils: any) =>
    new Promise<PreferencesDto | null>((resolve) => {
      APICallWrapper({
        url: apiUrls.foodAdviser.getPreferences,
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
    utils: any
  ): Promise<PreferencesDto> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: apiUrls.foodAdviser.updatePreferences,
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

  createSession: (utils: any): Promise<MenuSessionDto> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: apiUrls.foodAdviser.createSession,
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

  getSession: (sessionId: string, utils: any): Promise<MenuSessionDto | null> =>
    new Promise((resolve) => {
      APICallWrapper({
        url: `${apiUrls.foodAdviser.getSession}/${sessionId}`,
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

  uploadSessionImages: (
    sessionId: string,
    files: File[],
    utils: any
  ): Promise<string[]> =>
    new Promise((resolve, reject) => {
      const formData = new FormData();
      files.forEach((f) => formData.append("files", f));
      const url = `${apiUrls.foodAdviser.uploadImages}/${sessionId}/upload`;
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
    utils: any
  ): Promise<MenuSessionDto> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: `${apiUrls.foodAdviser.confirmMenu}/${sessionId}/confirm`,
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

  requestParsing: (sessionId: string, utils: any): Promise<void> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: `${apiUrls.foodAdviser.requestParsing}/${sessionId}/request-parsing`,
        options: { method: "POST" },
        utils,
        onSuccess: async () => resolve(),
        onFailure: async () => reject(),
        showError: true,
      });
    }),

  requestRecommendations: (sessionId: string, utils: any): Promise<void> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: `${apiUrls.foodAdviser.requestRecommendations}/${sessionId}/request-recommendations`,
        options: { method: "POST" },
        utils,
        onSuccess: async () => resolve(),
        onFailure: async () => reject(),
        showError: true,
      });
    }),

  getRecommendations: (
    sessionId: string,
    utils: any
  ): Promise<string[] | null> =>
    new Promise((resolve) => {
      APICallWrapper({
        url: `${apiUrls.foodAdviser.getRecommendations}/${sessionId}/recommendations`,
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
    utils: any
  ): Promise<void> =>
    new Promise((resolve, reject) => {
      APICallWrapper({
        url: apiUrls.foodAdviser.submitRating,
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
