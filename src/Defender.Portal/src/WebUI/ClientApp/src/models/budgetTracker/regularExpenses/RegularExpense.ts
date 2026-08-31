import { Currency } from "src/models/shared/Currency";

import { RegularExpenseType } from "./RegularExpenseType";

export interface RegularExpense {
  id: string;
  userId?: string;
  name: string;
  type: RegularExpenseType;
  currency: Currency;
  defaultAmount: number;
  orderPriority: number;
}

export interface RegularExpenseInput {
  name: string;
  type: RegularExpenseType;
  currency: Currency;
  defaultAmount: number;
  orderPriority: number;
}
