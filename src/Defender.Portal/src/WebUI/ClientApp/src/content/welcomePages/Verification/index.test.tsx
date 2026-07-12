import { act } from "react";
import { createRoot, Root } from "react-dom/client";
import { MemoryRouter } from "react-router-dom";

vi.mock("react-redux", () => ({
  connect: () => (component: unknown) => component,
}));

vi.mock("src/appUtils", () => ({
  default: () => ({
    react: { navigate: vi.fn() },
    t: (key: string) => key,
  }),
}));

vi.mock("src/api/APIWrapper/APICallWrapper", () => ({ default: vi.fn() }));
vi.mock("../Components/AuthPageShell", () => ({
  default: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));
vi.mock("src/components/LockedComponents/LockedButton/LockedButton", () => ({
  default: ({ children }: { children: React.ReactNode }) => <button>{children}</button>,
}));

import Verification from "./index";

describe("Verification polling", () => {
  let container: HTMLDivElement;
  let root: Root;

  beforeEach(() => {
    vi.useFakeTimers();
    container = document.createElement("div");
    document.body.appendChild(container);
    root = createRoot(container);
  });

  afterEach(() => {
    act(() => root.unmount());
    container.remove();
    vi.clearAllTimers();
    vi.useRealTimers();
  });

  test("WhenRerenderedAndUnmounted_OwnsOneIntervalAndClearsIt", () => {
    act(() => root.render(<MemoryRouter><Verification logout={vi.fn()} /></MemoryRouter>));
    expect(vi.getTimerCount()).toBe(1);

    act(() => root.render(<MemoryRouter><Verification logout={vi.fn()} /></MemoryRouter>));
    expect(vi.getTimerCount()).toBe(1);

    act(() => root.unmount());
    expect(vi.getTimerCount()).toBe(0);
  });
});
