import { getAbsoluteShareUrl } from "./ShareLink";

describe("getAbsoluteShareUrl", () => {
  test("WhenPublicUrlIsRelative_ReturnsUrlWithCurrentOrigin", () => {
    const url = getAbsoluteShareUrl(
      "/share/health-chart/token",
      "https://portal.example.com"
    );

    expect(url).toBe("https://portal.example.com/share/health-chart/token");
  });

  test("WhenPublicUrlIsAbsolute_ReturnsPublicUrl", () => {
    const url = getAbsoluteShareUrl(
      "https://shared.example.com/share/health-chart/token",
      "https://portal.example.com"
    );

    expect(url).toBe("https://shared.example.com/share/health-chart/token");
  });
});
