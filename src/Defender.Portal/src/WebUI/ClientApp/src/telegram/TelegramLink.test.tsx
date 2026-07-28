import { render, screen, waitFor } from "@testing-library/react";
import { Provider } from "react-redux";
import { MemoryRouter, Route, Routes } from "react-router-dom";

import TelegramLink, { createTelegramAccountLink } from "./TelegramLink";
import { getTelegramLaunchData, rememberTelegramLaunchData } from "./telegramLaunchContext";
import { loginActionName, logoutActionName } from "src/reducers/sessionReducer";
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

  test("createTelegramAccountLink_WhenSessionIsAuthenticated_PostsOnlyRawInitData", async () => {
    const fetchSpy = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchSpy);

    await expect(createTelegramAccountLink("query_id=AAEAA&hash=signed-value")).resolves.toBe("linked");

    expect(fetchSpy).toHaveBeenCalledWith("/api/telegram/link", {
      method: "POST",
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ initData: "query_id=AAEAA&hash=signed-value" }),
    });
  });

  test("WhenLinkedWithAuthenticatedPortalSession_RefreshesTelegramSessionAndNavigatesHome", async () => {
    const requestLink = vi.fn().mockResolvedValue("linked");
    const requestSession = vi.fn().mockResolvedValue({ kind: "authenticated", session });
    rememberTelegramLaunchData("query_id=AAEAA&hash=signed-value");
    store.dispatch({ type: loginActionName, payload: session });

    render(
      <Provider store={store}>
        <MemoryRouter initialEntries={["/telegram/link"]}>
          <Routes>
            <Route
              path="/telegram/link"
              element={<TelegramLink requestLink={requestLink} requestSession={requestSession} />}
            />
            <Route path="/home" element={<div>Home portal</div>} />
          </Routes>
        </MemoryRouter>
      </Provider>,
    );

    await waitFor(() => expect(requestLink).toHaveBeenCalledWith("query_id=AAEAA&hash=signed-value"));
    await waitFor(() => expect(requestSession).toHaveBeenCalledWith("query_id=AAEAA&hash=signed-value"));
    expect(await screen.findByText("Home portal")).not.toBeNull();
    expect(getTelegramLaunchData()).toBeNull();
  });
});
