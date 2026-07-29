import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { Provider } from "react-redux";

import TelegramShell, { TelegramLoading } from "./TelegramShell";
import store from "src/state/store";

describe("TelegramShell", () => {
  test("TelegramLoading_UsesPlainTextEllipsisWithoutMojibake", () => {
    render(<TelegramLoading />);

    expect(screen.getByText("Connecting your Telegram session...")).not.toBeNull();
  });

  test("WhenRendered_ProvidesEveryPortalDestinationInHorizontallyScrollableBottomNavigation", () => {
    render(
      <Provider store={store}>
        <MemoryRouter>
          <TelegramShell>
            <div>Portal content</div>
          </TelegramShell>
        </MemoryRouter>
      </Provider>,
    );

    expect(screen.getByRole("navigation", { name: "Telegram navigation" })).not.toBeNull();
    expect(screen.getByRole("link", { name: "Home" }).getAttribute("href")).toBe("/home");
    expect(screen.getByRole("link", { name: "Food Advisor" }).getAttribute("href")).toBe("/food-advisor");
    expect(screen.getByRole("link", { name: "Health Care" }).getAttribute("href")).toBe("/health-care");
    expect(screen.getByRole("link", { name: "Lottery" }).getAttribute("href")).toBe("/games/lottery");
    expect(screen.getByRole("link", { name: "Profile" }).getAttribute("href")).toBe("/account/update");
    expect(screen.getByLabelText("Language")).not.toBeNull();
    expect(screen.queryByRole("button", { name: "More" })).toBeNull();
  });

  test("WhenCurrentRouteIsPrimaryPortalDestination_MarksActiveTabAndKeepsBottomStripHorizontallyReachable", () => {
    render(
      <Provider store={store}>
        <MemoryRouter initialEntries={["/budget-tracker/positions"]}>
          <TelegramShell>
            <div>Portal content</div>
          </TelegramShell>
        </MemoryRouter>
      </Provider>,
    );

    const navigation = screen.getByRole("navigation", { name: "Telegram navigation" });

    expect(screen.getByRole("link", { name: "Budget" }).getAttribute("aria-current")).toBe("page");
    expect(screen.getByRole("link", { name: "Home" }).getAttribute("aria-current")).toBeNull();
    expect(getComputedStyle(navigation).overflowX).toBe("auto");
  });
});
