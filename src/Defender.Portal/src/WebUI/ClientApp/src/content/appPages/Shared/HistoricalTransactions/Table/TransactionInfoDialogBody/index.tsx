import { Divider, Grid } from "@mui/material";
import { format } from "date-fns";
import { connect } from "react-redux";

import useUtils from "src/appUtils";
import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";
import DateLocales from "src/consts/DateLocales";
import mapTransactionPurpose from "src/mappers/banking/mapTransactionPurpose";
import mapTransactionStatus from "src/mappers/banking/mapTransactionStatus";
import mapTransactionType from "src/mappers/banking/mapTransactionType";
import Transaction from "src/models/banking/Transaction";

const HorizontalDivider = () => {
  return (
    <Grid style={{ paddingTop: 0 }} size={12}>
      <Divider />
    </Grid>
  );
};

const TransactionInfoDialogBody = (props: any) => {
  const u = useUtils();

  const { currentLanguage, selectedTransaction: transaction } = props as {
    currentLanguage: string;
    selectedTransaction: Transaction;
  };

  if (!transaction) return <></>;

  const transactionPurpose = mapTransactionPurpose(
    u,
    transaction.transactionPurpose
  );

  return (
    <Grid
      container
      spacing={3}
      sx={{
        p: 2,
        justifyContent: "center",
        alignContent: "center",
        fontSize: "1.3em"
      }}>
      {transaction.transactionId && (
        <>
          <Grid
            size={{
              xs: 12,
              sm: 12
            }}>
            {transaction.transactionId}
          </Grid>
        </>
      )}
      {transactionPurpose && (
        <>
          {HorizontalDivider()}
          <Grid
            size={{
              xs: 12,
              sm: 12
            }}>
            {transactionPurpose}
          </Grid>
          {HorizontalDivider()}
        </>
      )}
      {transaction.transactionType && (
        <>
          {HorizontalDivider()}
          <Grid
            size={{
              xs: 6,
              sm: 5
            }}>
            {u.t("banking_page__trans_info_dialog_type_label")}:
          </Grid>
          <Grid
            size={{
              xs: 6,
              sm: 7
            }}>
            {mapTransactionType(u, transaction.transactionType)}
          </Grid>
        </>
      )}
      {transaction.transactionStatus && (
        <>
          {HorizontalDivider()}
          <Grid
            size={{
              xs: 6,
              sm: 5
            }}>
            {u.t("banking_page__trans_info_dialog_status_label")}:
          </Grid>
          <Grid
            size={{
              xs: 6,
              sm: 7
            }}>
            {mapTransactionStatus(u, transaction.transactionStatus)}
          </Grid>
        </>
      )}
      {transaction.fromWallet > 0 && (
        <>
          {HorizontalDivider()}
          <Grid
            size={{
              xs: 6,
              sm: 5
            }}>
            {u.t("banking_page__trans_info_dialog_from_label")}:
          </Grid>
          <Grid
            size={{
              xs: 6,
              sm: 7
            }}>
            {transaction.fromWallet}
          </Grid>
        </>
      )}
      {transaction.toWallet > 0 && (
        <>
          {HorizontalDivider()}
          <Grid
            size={{
              xs: 6,
              sm: 5
            }}>
            {u.t("banking_page__trans_info_dialog_to_label")}:
          </Grid>
          <Grid
            size={{
              xs: 6,
              sm: 7
            }}>
            {transaction.toWallet}
          </Grid>
        </>
      )}
      {transaction.amount && transaction.currency && (
        <>
          {HorizontalDivider()}
          <Grid
            size={{
              xs: 6,
              sm: 5
            }}>
            {u.t("banking_page__trans_info_dialog_amount_label")}:
          </Grid>
          <Grid
            size={{
              xs: 6,
              sm: 7
            }}>
            {transaction.amount / 100}{" "}
            {CurrencySymbolsMap[transaction.currency]}
          </Grid>
        </>
      )}
      {transaction.utcTransactionDate && (
        <>
          {HorizontalDivider()}
          <Grid
            size={{
              xs: 6,
              sm: 5
            }}>
            {u.t("banking_page__trans_info_dialog_date_label")}:
          </Grid>
          <Grid
            size={{
              xs: 6,
              sm: 7
            }}>
            {format(
              new Date(transaction.utcTransactionDate),
              "dd MMMM yyyy  hh:mm",
              {
                locale: DateLocales[currentLanguage],
              }
            )}
          </Grid>
          {HorizontalDivider()}
        </>
      )}
      {transaction.comment && (
        <>
          {HorizontalDivider()}
          <Grid
            size={{
              xs: 12,
              sm: 12
            }}>
            {u.t("banking_page__trans_info_dialog_comment_label")}:
            {"  " + transaction.comment}
          </Grid>
          {HorizontalDivider()}
        </>
      )}
    </Grid>
  );
};

const mapStateToProps = (state: any) => {
  return {
    currentLanguage: state.session.language,
  };
};

export default connect(mapStateToProps)(TransactionInfoDialogBody);
