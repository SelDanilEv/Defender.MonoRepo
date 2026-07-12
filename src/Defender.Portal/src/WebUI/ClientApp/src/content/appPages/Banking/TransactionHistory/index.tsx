import { Grid } from "@mui/material";
import HistoricalTransactions from "../../Shared/HistoricalTransactions";

const TransactionHistory = (props: any) => {
  return (
    <>
      <Grid container spacing={1}>
        <Grid size={12}>
          <HistoricalTransactions />
        </Grid>
      </Grid>
    </>
  );
};

export default TransactionHistory;
