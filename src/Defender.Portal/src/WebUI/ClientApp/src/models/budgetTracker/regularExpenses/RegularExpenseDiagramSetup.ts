import { Currency } from "src/models/shared/Currency";

export interface RegularExpenseDiagramSetup {
  id?: string;
  userId?: string;
  mainCurrency: Currency;
  lastMonths?: number;
  endMonth?: string;
}

export interface RegularExpenseChartRow {
  month: string;
  totalComfort: number;
  regular: number;
  subscription: number;
  annual: number;
}
