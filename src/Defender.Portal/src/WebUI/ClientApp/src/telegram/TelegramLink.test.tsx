import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { Provider } from "react-redux";
import { MemoryRouter, Route, Routes } from "react-router";

import TelegramLink, { createTelegramLinkHandoff } from "./TelegramLink";
import { getTelegramLaunchData, rememberTelegramLaunchData } from "./telegramLaunchContext";
import { logoutActionName } from "src/reducers/sessionReducer";
import store from "src/state/store";

const session = {
  isAuthenticated: true,
  language: "en",
  token: "",
  user: {
    id: "user-1",
    nickname: "Danil",
    email: "danil@example.com",
    phone: "+48123123123",
    isEmailVerified: true,
    isPhoneVerified: true,
    isBlocked: false,
    roles: ["User"],
    createdDate: new Date("2026-07-28T00:00:00Z"),
  },
};

describe("Telegram account linking", () => {
  afterEach(() => {
    store.dispatch({ type: logoutActionName });
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  test("createTelegramLinkHandoff_WhenMiniAppRequestsSignIn_SendsRawInitDataWithoutPuttingItInTheLoginUrl", async () => {
    const loginUrl = "https://portal.coded-by-danil.dev/welcome/login?TelegramHandoff=" + "a".repeat(64);
    const fetchSpy = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ code: "a".repeat(64), loginUrl }), { status: 200 }),
    );
    vi.stubGlobal("fetch", fetchSpy);

    await expect(createTelegramLinkHandoff("query_id=AAEAA&hash=signed-value")).resolves.toBe(loginUrl);

    expect(fetchSpy).toHaveBeenCalledWith("/api/telegram/link-handoff", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ initData: "query_id=AAEAA&hash=signed-value" }),
    });
  });

  test("WhenPortalCompletesNativeSignIn_RefreshesTelegramSessionAndNavigatesHome", async () => {
    const requestHandoff = vi.fn().mockResolvedValue("https://portal.coded-by-danil.dev/welcome/login?TelegramHandoff=" + "a".repeat(64));
    const requestSession = vi.fn().mockResolvedValue({ kind: "authenticated", session });
    rememberTelegramLaunchData("query_id=AAEAA&hash=signed-value");
    vi.stubGlobal("open", vi.fn());

    render(
      <Provider store={store}>
        <MemoryRouter initialEntries={["/telegram/link"]}>
          <Routes>
            <Route
              path="/telegram/link"
              element={<TelegramLink requestHandoff={requestHandoff} requestSession={requestSession} />}
            />
            <Route path="/home" element={<div>Home portal</div>} />
          </Routes>
        </MemoryRouter>
      </Provider>,
    );

    fireEvent.click(await screen.findByRole("button", { name: "Sign in to link" }));

    await waitFor(() => expect(requestHandoff).toHaveBeenCalledWith("query_id=AAEAA&hash=signed-value"));
    await waitFor(() => expect(requestSession).toHaveBeenCalledWith("query_id=AAEAA&hash=signed-value"));
    expect(await screen.findByText("Home portal")).not.toBeNull();
    expect(getTelegramLaunchData()).toBeNull();
  });
});
