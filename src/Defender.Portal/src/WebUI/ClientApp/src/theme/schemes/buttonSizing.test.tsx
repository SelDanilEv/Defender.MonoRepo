import { render, screen } from "@testing-library/react";
import { ThemeProvider } from "@mui/material/styles";
import { Button, ToggleButton, ToggleButtonGroup } from "@mui/material";

import { NebulaFighterTheme } from "./NebulaFighterTheme";

type SizeAffectingStyle = {
  minHeight: string;
  paddingTop: string;
  paddingRight: string;
  paddingBottom: string;
  paddingLeft: string;
  fontSize: string;
  lineHeight: string;
};

const readSizeAffectingStyle = (element: Element): SizeAffectingStyle => {
  const style = getComputedStyle(element);
  return {
    minHeight: style.minHeight,
    paddingTop: style.paddingTop,
    paddingRight: style.paddingRight,
    paddingBottom: style.paddingBottom,
    paddingLeft: style.paddingLeft,
    fontSize: style.fontSize,
    lineHeight: style.lineHeight,
  };
};

// Keys whose value can change a control's rendered footprint. Matches the plan's
// unit-invariance guard scope: fontSize, minHeight, height, padding/paddingLeft/Right/
// Top/Bottom, lineHeight, minWidth, width.
const SIZE_AFFECTING_KEY = /^(fontSize|minHeight|height|padding[A-Za-z]*|lineHeight|minWidth|width)$/;

// Recursively walks a theme component's styleOverrides tree (root, sizeSmall, nested
// "&:hover"-style selectors, etc.) and collects every value found under a size-affecting
// key, tagged with its path for a readable failure message.
const collectSizeAffectingValues = (
  node: unknown,
  path = "",
  collected: Array<{ path: string; value: unknown }> = [],
): Array<{ path: string; value: unknown }> => {
  if (!node || typeof node !== "object") {
    return collected;
  }

  Object.entries(node as Record<string, unknown>).forEach(([key, value]) => {
    const nextPath = path ? `${path}.${key}` : key;

    if (SIZE_AFFECTING_KEY.test(key) && (typeof value === "string" || typeof value === "number")) {
      collected.push({ path: nextPath, value });
    }

    if (value && typeof value === "object") {
      collectSizeAffectingValues(value, nextPath, collected);
    }
  });

  return collected;
};

describe("buttonSizing", () => {
  test("ToggleButton_WhenGroupedOrStandalone_ComputesSameGeometryAsPlainButton", () => {
    render(
      <ThemeProvider theme={NebulaFighterTheme}>
        <Button>Plain action</Button>
        <ToggleButtonGroup value="Declined" onChange={() => {}}>
          <ToggleButton value="Pending">Grouped toggle</ToggleButton>
        </ToggleButtonGroup>
        <ToggleButton value="standalone">Standalone toggle</ToggleButton>
      </ThemeProvider>,
    );

    const buttonStyle = readSizeAffectingStyle(screen.getByRole("button", { name: "Plain action" }));
    const groupedToggleStyle = readSizeAffectingStyle(screen.getByRole("button", { name: "Grouped toggle" }));
    const standaloneToggleStyle = readSizeAffectingStyle(screen.getByRole("button", { name: "Standalone toggle" }));

    // A grouped ToggleButton is sized via MuiToggleButtonGroup.defaultProps (size propagates
    // through React context to its children) while a bare ToggleButton is sized via
    // MuiToggleButton.defaultProps directly - exercising both required theme blocks.
    expect(groupedToggleStyle).toEqual(buttonStyle);
    expect(standaloneToggleStyle).toEqual(buttonStyle);

    // The unit match, not just the numeric value, is the load-bearing assertion: before this
    // fix ToggleButton computed minHeight "44px" and fontSize "0.8125rem" (MUI's own
    // pxToRem(13) default), not "30px"/"13px".
    expect(buttonStyle.minHeight).toBe("30px");
    expect(buttonStyle.fontSize).toBe("13px");
  });

  test("ThemeButtonFamilyStyleOverrides_ForSizeAffectingKeys_NeverUseRemOrEmUnits", () => {
    const components = (NebulaFighterTheme.components ?? {}) as Record<
      string,
      { styleOverrides?: unknown } | undefined
    >;
    const componentNames = ["MuiButton", "MuiToggleButton", "MuiIconButton", "MuiSvgIcon"] as const;

    const offenders = componentNames.flatMap((componentName) => {
      const styleOverrides = components[componentName]?.styleOverrides;

      return collectSizeAffectingValues(styleOverrides)
        .filter(({ value }) => typeof value === "string" && /(rem|em)$/.test(value))
        .map(({ path, value }) => `${componentName}.styleOverrides.${path}=${String(value)}`);
    });

    expect(offenders).toEqual([]);
  });

  test("MuiSvgIconStyleOverrides_ForEverySizeSlot_UsesPlainPixelNumbers", () => {
    const svgIconOverrides = NebulaFighterTheme.components?.MuiSvgIcon?.styleOverrides as
      | Record<string, { fontSize?: unknown } | undefined>
      | undefined;

    expect(svgIconOverrides?.fontSizeSmall).toEqual({ fontSize: 20 });
    expect(svgIconOverrides?.fontSizeMedium).toEqual({ fontSize: 24 });
    expect(svgIconOverrides?.fontSizeLarge).toEqual({ fontSize: 35 });
    expect(typeof svgIconOverrides?.fontSizeSmall?.fontSize).toBe("number");
    expect(typeof svgIconOverrides?.fontSizeMedium?.fontSize).toBe("number");
    expect(typeof svgIconOverrides?.fontSizeLarge?.fontSize).toBe("number");
  });
});
