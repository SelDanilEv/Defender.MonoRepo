import APICallProps, { APICallFailure } from "./interfaces/APICallProps";

import LoadingStateService from "src/services/LoadingStateService";
import SuccessToast from "src/components/Toast/DefaultSuccessToast";
import { beginSessionExpiryHandling, expireSession } from "src/services/SessionExpiryService";

const DEFAULT_TIMEOUT_MS = 30_000;

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

const parseFailure = async (response: Response): Promise<APICallFailure> => {
  const text = await response.text();
  if (!text) return { status: response.status, detail: response.statusText || "UnhandledError" };
  try {
    const problem = JSON.parse(text) as Partial<APICallFailure>;
    return { ...problem, status: response.status, detail: problem.detail || problem.title || "UnhandledError" };
  } catch {
    return { status: response.status, detail: text };
  }
};

const createRequestSignal = (callerSignal: AbortSignal | null | undefined, timeoutMs: number) => {
  const controller = new AbortController();
  const abortFromCaller = () => controller.abort(callerSignal?.reason ?? new DOMException("Aborted", "AbortError"));
  if (callerSignal?.aborted) abortFromCaller();
  else callerSignal?.addEventListener("abort", abortFromCaller, { once: true });
  const timer = window.setTimeout(
    () => controller.abort(new DOMException("RequestTimeout", "TimeoutError")),
    timeoutMs
  );
  return {
    signal: controller.signal,
    cleanup: () => {
      window.clearTimeout(timer);
      callerSignal?.removeEventListener("abort", abortFromCaller);
    },
  };
};

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
  timeoutMs = DEFAULT_TIMEOUT_MS,
  onSessionExpired,
}: APICallProps) => {
  let cleanupSignal = () => {};
  let responseReceived = false;
  try {
    if (doLock) LoadingStateService.StartLoading();

    const requestSignal = createRequestSignal(options.signal, timeoutMs);
    cleanupSignal = requestSignal.cleanup;
    const requestOptions = getRequestOptions({ ...options, signal: requestSignal.signal });
    const response = await fetch(url, requestOptions);
    responseReceived = true;

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

    const failure = await parseFailure(response);
    switch (response.status) {
      case 401:
        if (onSessionExpired) {
          if (beginSessionExpiryHandling()) await onSessionExpired();
        } else {
          await expireSession(utils);
        }
        await onFailure(failure);
        break;
      case 403:
        await onFailure(failure);
        utils?.e("ForbiddenAccess");
        break;
      default:
        await onFailure(failure);

        if (showError) {
          if (utils) utils.e(failure.detail || "UnhandledError");
          break;
        }
    }
  } catch (error) {
    if (responseReceived) throw error;
    const signalReason = error instanceof DOMException ? error.name : "";
    const detail = signalReason === "TimeoutError"
      ? "RequestTimeout"
      : signalReason === "AbortError" || options.signal?.aborted
        ? "RequestCancelled"
        : getErrorDetail(error);
    const failure = { ...toFailure(error), detail };
    await onFailure(failure);
    if (utils && detail !== "RequestCancelled") utils.e(detail);
  } finally {
    cleanupSignal();
    try {
      await onFinal();
    } finally {
      if (doLock) LoadingStateService.FinishLoading();
    }
  }
};

export default APICallWrapper;
