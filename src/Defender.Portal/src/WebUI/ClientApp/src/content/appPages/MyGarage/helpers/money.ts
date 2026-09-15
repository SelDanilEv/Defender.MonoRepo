import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";
import type { Currency } from "src/models/myGarage/CarModels";

export const majorToMinor = (value: string | number): number | null => {
  if (typeof value === "number") {
    return Number.isFinite(value) && value >= 0 ? Math.round(value * 100) : null;
  }

  const normalized = value.trim().replace(",", ".");
  if (!normalized) return null;

  const parsed = Number(normalized);
  return Number.isFinite(parsed) && parsed >= 0 ? Math.round(parsed * 100) : null;
};

export const minorToMajor = (amount: number | null | undefined): string =>
  amount == null || !Number.isFinite(amount) ? "" : (amount / 100).toFixed(2);

export const formatMinorCost = (amount: number | null | undefined, currency: Currency | null | undefined): string => {
  if (amount == null || currency == null) return "";
  const symbol = CurrencySymbolsMap[currency] || currency;
  return `${minorToMajor(amount)} ${symbol}`;
};
