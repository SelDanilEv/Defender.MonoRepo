import { render, screen } from "@testing-library/react";

import SuspenseLoader from "./index";

describe("SuspenseLoader", () => {
  test("WhenLoadingPage_ExposesAccessibleProgressbarName", () => {
    render(<SuspenseLoader />);

    expect(screen.getByRole("progressbar", { name: "Loading page" })).not.toBeNull();
  });
});
