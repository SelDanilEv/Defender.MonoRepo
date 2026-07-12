import { useState } from "react";
import { Grid } from "@mui/material";
import HistoricalTickets from "./HistoricalTickets";
import LotteryTicket from "src/models/games/lottery/LotteryTicket";
import LatestTickets from "./LatestTickets";
import useUtils from "src/appUtils";
import ActiveDraws from "./ActiveDraws";

const LotteryHomePage = (props: any) => {
  const u = useUtils();
  const [latestTickets, setLatestTickets] = useState<LotteryTicket[]>([]);

  const SetLatestTickets = (tickets: LotteryTicket[]) => {
    const latestTickets = tickets.slice(0, 10);
    setLatestTickets(latestTickets);
  };

  return (
    <>
      <Grid container spacing={1}>
        <Grid
          container
          spacing={1}
          sx={{ order: { xs: 2, sm: 1 } }}
          size={{
            xs: 12,
            sm: 8
          }}>
          <Grid size={12}>
            <ActiveDraws />
          </Grid>
          <Grid size={12}>
            <HistoricalTickets SetLatestTickets={SetLatestTickets} />
          </Grid>
        </Grid>
        <Grid
          sx={{ order: { xs: 1, sm: 2 } }}
          size={{
            xs: 12,
            sm: 4
          }}>
          <LatestTickets LatestTickets={latestTickets} />
        </Grid>
      </Grid>
    </>
  );
};

export default LotteryHomePage;
