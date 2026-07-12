import { render, screen } from "@testing-library/react";
import { ThemeProvider } from "@mui/material/styles";

import Logo from "./index";
import { NebulaFighterTheme } from "src/theme/schemes/NebulaFighterTheme";

describe("Logo", () => {
  test("renders current frontend version as a native version tag", () => {
    render(
      <ThemeProvider theme={NebulaFighterTheme}>
        <Logo height={72} width={72} />
      </ThemeProvider>
    );

    expect(screen.getByLabelText("Frontend version 1.3").textContent).toBe("v1.3");
  });
});
