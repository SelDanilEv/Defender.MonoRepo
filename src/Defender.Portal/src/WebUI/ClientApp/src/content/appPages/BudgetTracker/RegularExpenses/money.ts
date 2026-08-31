export const minorToMajor = (amount: number): string => {
  if (!Number.isFinite(amount)) return "0";
  return (amount / 100).toString();
};

export const majorToMinor = (value: string | number): number | null => {
  if (typeof value === "number") {
    return Number.isFinite(value) && value >= 0 ? Math.round(value * 100) : null;
  }

  const normalized = value.trim().replace(",", ".");
  if (!normalized) return null;

  const parsed = Number(normalized);
  return Number.isFinite(parsed) && parsed >= 0 ? Math.round(parsed * 100) : null;
};

export const displayAmount = (amount: number, currency: string, symbol: string): string =>
  `${minorToMajor(amount)} ${symbol || currency}`;
