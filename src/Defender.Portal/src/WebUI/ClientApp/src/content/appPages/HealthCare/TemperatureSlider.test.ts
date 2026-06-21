import { normalizeTemperature } from "./TemperatureSlider";

describe("temperature slider helpers", () => {
  test("normalizeTemperature_WhenValueIsBelowMinimum_ReturnsMinimum", () => {
    expect(normalizeTemperature("35.9")).toBe(36.4);
  });

  test("normalizeTemperature_WhenValueIsAboveMaximum_ReturnsMaximum", () => {
    expect(normalizeTemperature("41")).toBe(40.5);
  });

  test("normalizeTemperature_WhenValueIsInvalid_ReturnsDefault", () => {
    expect(normalizeTemperature("not-a-number")).toBe(37);
  });
});
