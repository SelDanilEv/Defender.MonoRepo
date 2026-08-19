import { fireEvent, render, screen } from "@testing-library/react";
import type { ComponentType } from "react";

const navigate = vi.fn();
const locationState = vi.hoisted(() => vi.fn());

vi.mock("react-redux", () => ({
  connect: () => (component: unknown) => component,
}));

vi.mock("src/appUtils", () => ({
  default: () => ({
    react: {
      locationState,
      navigate,
      theme: {
        colors: {
          primary: { lighter: "rgba(100, 100, 200, 0.2)" },
          info: { main: "#1976d2" },
          warning: { main: "#ed6c02" },
          alpha: { trueWhite: { 100: "#fff", 70: "#fff", 50: "#fff", 30: "#fff", 10: "#fff", 5: "#fff" } },
          gradients: {
            purple3: "linear-gradient(135deg, #667eea, #764ba2)",
            blue3: "linear-gradient(135deg, #1976d2, #512da8)",
          },
          shadows: { primary: "none" },
        },
        palette: {
          background: { paper: "#fff" },
          primary: { contrastText: "#fff" },
          warning: { main: "#ed6c02" },
        },
      },
    },
    t: (key: string) => key,
  }),
}));

vi.mock("./SelectAndPayPanel", () => ({
  default: () => <div>ticket selection panel</div>,
}));

vi.mock("src/components/LockedComponents/LockedButton/LockedButton", () => ({
  default: ({ children, ...props }: { children: React.ReactNode; onClick?: () => void }) => (
    <button {...props}>{children}</button>
  ),
}));

import ConnectedPurchaseTickets from ".";

const PurchaseTickets = ConnectedPurchaseTickets as unknown as ComponentType<{
  currentLanguage: string;
}>;

describe("PurchaseTickets", () => {
  beforeEach(() => {
    navigate.mockReset();
    locationState.mockImplementation(() => {
      throw new TypeError("Cannot read properties of null (reading 'draw')");
    });
  });

  test("WhenOpenedWithoutDrawState_RendersSafeReturnStateInsteadOfThrowing", () => {
    render(<PurchaseTickets currentLanguage="en" />);

    expect(screen.getByRole("heading", { name: "lottery:draw_not_selected_title" })).not.toBeNull();
    expect(screen.getByRole("button", { name: "lottery:draw_not_selected_back_button" })).not.toBeNull();
  });

  test("WhenSafeReturnStateBackButtonClicked_NavigatesToLotteryHome", () => {
    render(<PurchaseTickets currentLanguage="en" />);

    fireEvent.click(screen.getByRole("button", { name: "lottery:draw_not_selected_back_button" }));

    expect(navigate).toHaveBeenCalledWith("/games/lottery");
  });

  test("WhenOpenedWithDrawState_RendersTicketSelectionPanel", () => {
    locationState.mockReturnValue({
      drawNumber: 1,
      publicNames: { en: "Family lottery" },
      endDate: "2026-08-19T23:00:00Z",
      coefficients: [300, 150, 70],
      allowedBets: [100],
      allowedCurrencies: ["USD"],
      minBetValue: 100,
      maxBetValue: 500,
    });

    render(<PurchaseTickets currentLanguage="en" />);

    expect(screen.getByText("ticket selection panel")).not.toBeNull();
  });
});
