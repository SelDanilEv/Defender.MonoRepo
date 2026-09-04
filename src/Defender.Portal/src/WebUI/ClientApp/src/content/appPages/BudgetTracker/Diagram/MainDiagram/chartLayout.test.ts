import { getMainDiagramLayout } from "./chartLayout";

describe("getMainDiagramLayout", () => {
  test("WhenMobile_UsesOnlyAxisSpaceInsideTheChartCanvas", () => {
    expect(
      getMainDiagramLayout({
        isMobile: true,
        isLargeScreen: false,
        additionalLegendMargin: 13,
      })
    ).toEqual({
      height: 413,
      margin: { top: 0, bottom: 0, left: 0, right: 0 },
    });
  });

  test("WhenLaptop_UsesTheSameCompactSpacingAsMobile", () => {
    expect(
      getMainDiagramLayout({
        isMobile: false,
        isLargeScreen: false,
        additionalLegendMargin: 20,
      })
    ).toEqual({
      height: 470,
      margin: { top: 0, bottom: 0, left: 0, right: 0 },
    });
  });

  test("WhenLargeScreen_PreservesExistingChartSpacing", () => {
    expect(
      getMainDiagramLayout({
        isMobile: false,
        isLargeScreen: true,
        additionalLegendMargin: 20,
      })
    ).toEqual({
      height: 720,
      margin: { top: 10, bottom: 85, left: 68, right: 40 },
    });
  });
});
