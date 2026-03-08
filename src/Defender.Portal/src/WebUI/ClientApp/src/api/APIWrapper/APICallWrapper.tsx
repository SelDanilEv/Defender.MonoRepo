import APICallProps, { APICallFailure } from "./interfaces/APICallProps";

import LoadingStateService from "src/services/LoadingStateService";
import SuccessToast from "src/components/Toast/DefaultSuccessToast";

const getRequestOptions = (options: RequestInit): RequestInit => {
  const headers = new Headers(options.headers);

  if (!headers.has("Content-Type") && !(options.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  return {
    ...options,
    credentials: options.credentials ?? "same-origin",
    headers,
  };
};

const getErrorDetail = (error: unknown): string => {
  if (
    typeof error === "object" &&
    error !== null &&
    "detail" in error &&
    typeof error.detail === "string"
  ) {
    return error.detail;
  }

  return "UnhandledError";
};

const toFailure = (error: unknown): APICallFailure => ({
  status: 0,
  detail: getErrorDetail(error),
});

const APICallWrapper = async ({
  url,
  options,
  utils = null,
  onSuccess = async (response) => {},
  onFailure = async (response) => {},
  onFinal = async () => {},
  showSuccess = false,
  successMessage = undefined,
  showError = true,
  doLock = true,
}: APICallProps) => {
  try {
    if (doLock) LoadingStateService.StartLoading();

    const requestOptions = getRequestOptions(options);
    const response = await fetch(url, requestOptions);

    if (response.ok) {
        await onSuccess(response);

        if (showSuccess || successMessage) {
          if (!successMessage && utils != null) {
            successMessage = utils.t("Notification_Success");
          }

          SuccessToast(successMessage);
        }
        return;
    }

    switch (response.status) {
      case 401:
        await onFailure(response);
        break;
      case 403:
        utils?.e("ForbiddenAccess");
        break;
      default:
        await onFailure(response);

        if (showError) {
          const errorText = await response.text();
          let errorDetail = "UnhandledError";

          if (errorText) {
            try {
              const error = JSON.parse(errorText);
              errorDetail = error?.detail || errorDetail;
            } catch {
              errorDetail = errorText;
            }
          }

          if (utils) utils.e(errorDetail);
          break;
        }
    }
  } catch (error) {
    console.error(error);
    await onFailure(toFailure(error));
    if (utils) utils.e(getErrorDetail(error));
  } finally {
    await onFinal();
    if (doLock) LoadingStateService.FinishLoading();
  }
};

export default APICallWrapper;
