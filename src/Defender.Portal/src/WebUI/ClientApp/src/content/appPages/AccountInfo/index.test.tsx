import { render, screen } from "@testing-library/react";

import { AccountInfoPage } from "./index";

vi.mock("./EditUserInfo", () => ({ default: () => <div>Basic account editor</div> }));
vi.mock("./EditSensitiveUserInfo", () => ({ default: () => <div>Sensitive account editor</div> }));
vi.mock("src/appUtils", () => ({
  default: () => ({ react: { navigate: vi.fn() }, t: (key: string) => key }),
}));

describe("AccountInfoPage", () => {
  test("WhenTelegramSessionHasNoNickname_RendersProfileInsteadOfThrowing", () => {
    render(<AccountInfoPage currentUser={{ nickname: null }} />);

    expect(screen.getByText("Profile")).not.toBeNull();
    expect(screen.getByText("Basic account editor")).not.toBeNull();
  });
});
