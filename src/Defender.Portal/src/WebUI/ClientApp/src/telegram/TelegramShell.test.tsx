import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";

import TelegramShell, { TelegramLoading } from "./TelegramShell";

describe("TelegramShell", () => {
  test("TelegramLoading_UsesPlainTextEllipsisWithoutMojibake", () => {
    render(<TelegramLoading />);

    expect(screen.getByText("Connecting your Telegram session...")).not.toBeNull();
  });

  test("WhenRendered_ProvidesCompactBottomNavigationAndMorePortalDestinations", () => {
    render(
      <MemoryRouter>
        <TelegramShell>
          <div>Portal content</div>
        </TelegramShell>
      </MemoryRouter>,
    );

    expect(screen.getByRole("navigation", { name: "Telegram navigation" })).not.toBeNull();
    expect(screen.getByRole("link", { name: "Home" }).getAttribute("href")).toBe("/home");
    expect(screen.getByRole("button", { name: "More" })).not.toBeNull();
  });
});
