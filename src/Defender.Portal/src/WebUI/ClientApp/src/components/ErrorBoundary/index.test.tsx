import React from "react";
import ReactDOM from "react-dom";
import { act } from "react-dom/test-utils";

import ErrorBoundary from "./index";

describe("ErrorBoundary", () => {
  let container: HTMLDivElement;

  beforeEach(() => {
    container = document.createElement("div");
    document.body.appendChild(container);
    jest.spyOn(console, "error").mockImplementation(() => undefined);
  });

  afterEach(() => {
    ReactDOM.unmountComponentAtNode(container);
    container.remove();
    jest.restoreAllMocks();
  });

  test("WhenChildThrows_RendersFallback", () => {
    const BrokenComponent = () => {
      throw new Error("boom");
    };

    act(() => {
      ReactDOM.render(
        <ErrorBoundary>
          <BrokenComponent />
        </ErrorBoundary>,
        container
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
      ReactDOM.render(
        <ErrorBoundary>
          <SometimesBrokenComponent />
        </ErrorBoundary>,
        container
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
