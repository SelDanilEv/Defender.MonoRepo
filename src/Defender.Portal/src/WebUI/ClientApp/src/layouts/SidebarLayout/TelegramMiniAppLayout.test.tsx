import { render, screen, waitFor } from "@testing-library/react";
import { Provider } from "react-redux";
import { MemoryRouter, Route, Routes } from "react-router-dom";

import SidebarLayout from ".";
import { loginActionName, logoutActionName } from "src/reducers/sessionReducer";
import store from "src/state/store";
import ThemeProvider from "src/theme/ThemeProvider";
import { SidebarProvider } from "src/contexts/SidebarContext";
import type { TelegramWebApp } from "src/telegram/telegramWebApp";

describe("SidebarLayout Telegram Mini App mode", () => {
  afterEach(() => {
    delete (window as Window & { Telegram?: unknown }).Telegram;
    store.dispatch({ type: logoutActionName });
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  test("WhenTelegramRuntimeIsPresent_UsesCompactNavigationForExistingPortalRoute", async () => {
    store.dispatch({
      type: loginActionName,
      payload: {
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
      },
    });
    setTelegramRuntime();
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 204 })));

    render(
      <Provider store={store}>
        <ThemeProvider>
          <SidebarProvider>
            <MemoryRouter initialEntries={["/home"]}>
              <Routes>
                <Route element={<SidebarLayout />}>
                  <Route path="/home" element={<div>Home portal content</div>} />
                </Route>
              </Routes>
            </MemoryRouter>
          </SidebarProvider>
        </ThemeProvider>
      </Provider>,
    );

    expect(screen.getByText("Home portal content")).not.toBeNull();
    await waitFor(() => expect(screen.getByRole("navigation", { name: "Telegram navigation" })).not.toBeNull());
    expect(screen.getByRole("link", { name: "Wallet" }).getAttribute("href")).toBe("/banking");
  });
});

const setTelegramRuntime = (): void => {
  const runtime: TelegramWebApp = {
    initData: "query_id=AAEAA&hash=signed-value",
    ready: vi.fn(),
    expand: vi.fn(),
    onEvent: vi.fn(),
    offEvent: vi.fn(),
  };
  (window as Window & { Telegram?: { WebApp?: TelegramWebApp } }).Telegram = { WebApp: runtime };
};
