import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import LoadingStateService from "src/services/LoadingStateService";
import { resetSessionExpiryHandling } from "src/services/SessionExpiryService";

const response = (status: number, body = "") =>
  new Response(body, {
    status,
    headers: { "Content-Type": "application/problem+json" },
  });

describe("APICallWrapper", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    resetSessionExpiryHandling();
  });

  test("WhenCalled_SetsSameOriginCredentialsAndDoesNotSetAuthorizationHeader", async () => {
    const fetchMock = vi.fn().mockResolvedValue(response(200));
    vi.stubGlobal("fetch", fetchMock);

    await APICallWrapper({ url: "/health", options: { method: "GET" }, doLock: false });

    const options = fetchMock.mock.calls[0][1] as RequestInit;
    expect(options.credentials).toBe("same-origin");
    expect(new Headers(options.headers).has("Authorization")).toBe(false);
  });

  test("WhenUnauthorized_ExpiresSessionOnceAndReturnsProblemDetails", async () => {
    vi.stubGlobal("fetch", vi.fn().mockImplementation(async () => response(401, JSON.stringify({
      title: "Unauthorized",
      detail: "Cookie expired",
      traceId: "trace-1",
    }))));
    const onSessionExpired = vi.fn();
    const onFailure = vi.fn();

    await APICallWrapper({ url: "/one", options: {}, doLock: false, onSessionExpired, onFailure });
    await APICallWrapper({ url: "/two", options: {}, doLock: false, onSessionExpired, onFailure });

    expect(onSessionExpired).toHaveBeenCalledTimes(1);
    expect(onFailure).toHaveBeenLastCalledWith(expect.objectContaining({
      status: 401,
      title: "Unauthorized",
      detail: "Cookie expired",
      traceId: "trace-1",
    }));
  });

  test("WhenRequestTimesOut_AbortsFetchAndReportsTimeout", async () => {
    vi.useFakeTimers();
    const fetchMock = vi.fn((_url: string, options: RequestInit) =>
      new Promise((_resolve, reject) => {
        options.signal?.addEventListener("abort", () => reject(options.signal?.reason));
      }));
    vi.stubGlobal("fetch", fetchMock);
    const onFailure = vi.fn();

    const call = APICallWrapper({ url: "/slow", options: {}, timeoutMs: 50, doLock: false, onFailure });
    await vi.advanceTimersByTimeAsync(50);
    await call;

    expect(fetchMock.mock.calls[0][1].signal?.aborted).toBe(true);
    expect(onFailure).toHaveBeenCalledWith(expect.objectContaining({ status: 0, detail: "RequestTimeout" }));
    vi.useRealTimers();
  });

  test("WhenCallerAborts_PropagatesCancellationSignal", async () => {
    const controller = new AbortController();
    const fetchMock = vi.fn((_url: string, options: RequestInit) => {
      controller.abort("left-page");
      return Promise.reject(options.signal?.reason);
    });
    vi.stubGlobal("fetch", fetchMock);
    const onFailure = vi.fn();

    await APICallWrapper({ url: "/cancel", options: { signal: controller.signal }, doLock: false, onFailure });

    expect(fetchMock.mock.calls[0][1].signal?.aborted).toBe(true);
    expect(onFailure).toHaveBeenCalledWith(expect.objectContaining({ status: 0, detail: "RequestCancelled" }));
  });

  test("WhenCallbacksThrow_AlwaysFinishesLoadingAndRunsFinalCallback", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(response(200)));
    vi.spyOn(LoadingStateService, "StartLoading").mockImplementation(() => undefined);
    vi.spyOn(LoadingStateService, "FinishLoading").mockImplementation(() => undefined);
    const onFinal = vi.fn();

    await expect(APICallWrapper({ url: "/health", options: {}, onSuccess: () => { throw new Error("boom"); }, onFinal })).rejects.toThrow("boom");

    expect(onFinal).toHaveBeenCalledTimes(1);
    expect(LoadingStateService.FinishLoading).toHaveBeenCalledTimes(1);
  });

  test("WhenSuccessCallbackThrows_DoesNotMisclassifyItAsTransportFailure", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(response(200)));
    const onFailure = vi.fn();

    await expect(APICallWrapper({
      url: "/health",
      options: {},
      doLock: false,
      onSuccess: () => { throw new Error("consumer bug"); },
      onFailure,
    })).rejects.toThrow("consumer bug");

    expect(onFailure).not.toHaveBeenCalled();
  });
});
