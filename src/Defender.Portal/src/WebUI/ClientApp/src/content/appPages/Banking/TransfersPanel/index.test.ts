import { describe, expect, test } from "vitest";

import { calculateTransferAllowed } from "./index";

describe("calculateTransferAllowed", () => {
  test("WhenSelectedCurrencyAccountIsMissing_ReturnsFalse", () => {
    expect(calculateTransferAllowed(123456, 25, "USD", [])).toBe(false);
  });
});
