import { act, render, waitFor } from "@testing-library/react";
import { toast } from "react-toastify";

import AppToastContainer from "./index";

describe("AppToastContainer", () => {
  test("RendersAboveModalLayer", async () => {
    render(<AppToastContainer />);
    act(() => {
      toast.error("test");
    });

    await waitFor(() => expect(document.querySelector(".Toastify__toast-container--top-right")).not.toBeNull());
    const toastContainer = document.querySelector<HTMLElement>(".Toastify__toast-container--top-right");

    expect(toastContainer?.style.zIndex).toBe("9999");
  });
});
