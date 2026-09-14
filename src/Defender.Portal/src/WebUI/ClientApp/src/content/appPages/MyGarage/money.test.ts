import { describe, expect, test } from "vitest";

import { Currency } from "src/models/myGarage/CarModels";

import { formatMinorCost, majorToMinor, minorToMajor } from "./helpers/money";

describe("My Garage money helpers", () => {
  test.each([
    ["12.345", 1235],
    ["1,005", 100],
    [100, 10000],
  ])("majorToMinor_WhenGivenMajorUnits_RoundsToMinorUnits", (input, expected) => {
    expect(majorToMinor(input)).toBe(expected);
  });

  test("majorToMinor_WhenBlank_ReturnsNullPairValue", () => {
    expect(majorToMinor("  ")).toBeNull();
    expect(formatMinorCost(null, null)).toBe("");
  });

  test("minorToMajor_WhenGivenOneUnit_UsesTwoDecimalPlaces", () => {
    expect(minorToMajor(100)).toBe("1.00");
    expect(formatMinorCost(1234, Currency.PLN)).toBe("12.34 zł");
  });
});
