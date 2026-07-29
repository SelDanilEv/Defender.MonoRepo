import { getMainDiagramLayout } from "./chartLayout";

describe("getMainDiagramLayout", () => {
  test("WhenMobile_UsesCompactAxisAndLegendMargins", () => {
    expect(
      getMainDiagramLayout({
        isMobile: true,
        isLargeScreen: false,
        additionalLegendMargin: 13,
      })
    ).toEqual({
      height: 413,
      margin: { top: 10, bottom: 55, left: 44, right: 12 },
    });
  });

  test("WhenDesktop_PreservesExistingChartSpacing", () => {
    expect(
      getMainDiagramLayout({
        isMobile: false,
        isLargeScreen: false,
        additionalLegendMargin: 20,
      })
    ).toEqual({
      height: 470,
      margin: { top: 10, bottom: 85, left: 68, right: 40 },
    });
  });
});
