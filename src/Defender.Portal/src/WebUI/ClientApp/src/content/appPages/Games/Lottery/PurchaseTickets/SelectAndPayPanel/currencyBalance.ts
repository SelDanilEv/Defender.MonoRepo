import { CurrencyAccount } from "src/models/banking/WalletInfo";
import { Currency } from "src/models/shared/Currency";

export const getCurrencyAccountBalance = (
  currency: Currency,
  currencyAccounts?: CurrencyAccount[]
): number | undefined =>
  currencyAccounts?.find((account) => account.currency === currency)?.balance;
