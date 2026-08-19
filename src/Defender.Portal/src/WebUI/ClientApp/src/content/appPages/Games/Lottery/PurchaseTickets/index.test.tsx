import { fireEvent, render, screen } from "@testing-library/react";
import type { ComponentType } from "react";

const navigate = vi.fn();

vi.mock("react-redux", () => ({
  connect: () => (component: unknown) => component,
}));

vi.mock("src/appUtils", () => ({
  default: () => ({
    react: {
      locationState: () => null,
      navigate,
      theme: {
        colors: {
          primary: { lighter: "rgba(100, 100, 200, 0.2)" },
          gradients: { purple3: "linear-gradient(135deg, #667eea, #764ba2)" },
          shadows: { primary: "none" },
        },
        palette: { background: { paper: "#fff" }, primary: { contrastText: "#fff" } },
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
});
