import { act } from "react";
import { createRoot, Root } from "react-dom/client";

import ErrorBoundary from "./index";

describe("ErrorBoundary", () => {
  let container: HTMLDivElement;
  let root: Root;

  beforeEach(() => {
    container = document.createElement("div");
    document.body.appendChild(container);
    root = createRoot(container);
    vi.spyOn(console, "error").mockImplementation(() => undefined);
  });

  afterEach(() => {
    act(() => root.unmount());
    container.remove();
    vi.restoreAllMocks();
  });

  test("WhenChildThrows_RendersFallback", () => {
    const BrokenComponent = () => {
      throw new Error("boom");
    };

    act(() => {
      root.render(
        <ErrorBoundary>
          <BrokenComponent />
        </ErrorBoundary>,
      );
    });

    expect(container.textContent).toContain("Something went wrong");
    expect(container.textContent).toContain(
      "The page hit an unexpected client-side error."
    );
  });

  test("WhenRetryClickedAfterFailureAndChildRecovers_RendersChildren", () => {
    let shouldThrow = true;

    const SometimesBrokenComponent = () => {
      if (shouldThrow) {
        throw new Error("boom");
      }

      return <div>Recovered content</div>;
    };

    act(() => {
      root.render(
        <ErrorBoundary>
          <SometimesBrokenComponent />
        </ErrorBoundary>,
      );
    });

    shouldThrow = false;

    const retryButton = container.querySelector("button");

    expect(retryButton).not.toBeNull();

    act(() => {
      retryButton?.dispatchEvent(new MouseEvent("click", { bubbles: true }));
    });

    expect(container.textContent).toContain("Recovered content");
  });
});
