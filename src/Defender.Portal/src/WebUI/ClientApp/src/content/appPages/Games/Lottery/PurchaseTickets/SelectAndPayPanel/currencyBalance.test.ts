import { describe, expect, test } from "vitest";
import { Currency } from "src/models/shared/Currency";
import { getCurrencyAccountBalance } from "./currencyBalance";

describe("getCurrencyAccountBalance", () => {
  test("returns balance for selected currency", () => {
    expect(
      getCurrencyAccountBalance(Currency.EUR, [
        { currency: Currency.USD, balance: 12345 },
        { currency: Currency.EUR, balance: 678 },
      ])
    ).toBe(678);
  });

  test("returns undefined when selected currency account is missing", () => {
    expect(
      getCurrencyAccountBalance(Currency.EUR, [
        { currency: Currency.USD, balance: 12345 },
      ])
    ).toBeUndefined();
  });
});
