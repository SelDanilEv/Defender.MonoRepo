import BoltIcon from "@mui/icons-material/Bolt";
import { Box, Card, Chip, Grid, Typography } from "@mui/material";
import { useState } from "react";

import useUtils from "src/appUtils";
import LotteryDraw from "src/models/games/lottery/LotteryDraw";
import LotteryTicket from "src/models/games/lottery/LotteryTicket";
import ActiveDraws from "./ActiveDraws";
import HistoricalTickets from "./HistoricalTickets";
import LatestTickets from "./LatestTickets";
import {
  getLotteryArenaHeroTextColors,
  getLotteryArenaStats,
} from "./lotteryPresentation";

const LotteryHomePage = () => {
  const u = useUtils();
  const theme = u.react.theme;
  const [latestTickets, setLatestTicketsState] = useState<LotteryTicket[]>([]);
  const [activeDraws, setActiveDraws] = useState<LotteryDraw[]>([]);
  const stats = getLotteryArenaStats(activeDraws, latestTickets);
  const heroTextColors = getLotteryArenaHeroTextColors(theme);

  const updateLatestTickets = (tickets: LotteryTicket[]) => {
    setLatestTicketsState(tickets.slice(0, 10));
  };

  return (
    <Box sx={{ maxWidth: 1480, mx: "auto", pb: 2 }}>
      <Card
        data-testid="lottery-arena-hero"
        sx={{
          p: { xs: 2, md: 3 },
          position: "relative",
          overflow: "hidden",
          color: theme.colors.alpha.trueWhite[100],
          background: `radial-gradient(circle at 82% 18%, ${theme.colors.info.main}44, transparent 28%), radial-gradient(circle at 10% 105%, ${theme.colors.primary.main}66, transparent 36%), ${theme.colors.gradients.blue3}`,
          boxShadow: theme.colors.shadows.primary,
          "&::before": {
            content: '""',
            position: "absolute",
            inset: 0,
            opacity: 0.55,
            backgroundImage: `linear-gradient(${theme.colors.alpha.trueWhite[5]} 1px, transparent 1px), linear-gradient(90deg, ${theme.colors.alpha.trueWhite[5]} 1px, transparent 1px)`,
            backgroundSize: "34px 34px",
            maskImage: "linear-gradient(to bottom right, black, transparent 70%)",
          },
          "&::after": {
            content: '""',
            position: "absolute",
            width: 220,
            height: 220,
            right: -70,
            bottom: -110,
            borderRadius: "50%",
            border: `1px solid ${theme.colors.alpha.trueWhite[30]}`,
            boxShadow: `0 0 0 20px ${theme.colors.alpha.trueWhite[5]}, 0 0 0 40px ${theme.colors.alpha.trueWhite[5]}`,
          },
        }}
      >
        <Grid container spacing={2} sx={{ position: "relative", zIndex: 1 }}>
          <Grid size={{ xs: 12, md: 7 }}>
            <Chip
              icon={<BoltIcon />}
              label={u.t("lottery:arena_live_badge")}
              sx={{
                mb: 1.5,
                color: theme.colors.alpha.trueWhite[100],
                backgroundColor: theme.colors.alpha.trueWhite[10],
                border: `1px solid ${theme.colors.alpha.trueWhite[30]}`,
                "& .MuiChip-icon": {
                  color: theme.colors.warning.main,
                  animation: "lottery-pulse 1.8s ease-in-out infinite",
                },
                "@keyframes lottery-pulse": {
                  "0%, 100%": { opacity: 0.6, transform: "scale(0.9)" },
                  "50%": { opacity: 1, transform: "scale(1.1)" },
                },
                "@media (prefers-reduced-motion: reduce)": {
                  "& .MuiChip-icon": { animation: "none" },
                },
              }}
            />
            <Typography variant="h2" sx={{ fontWeight: 900, letterSpacing: "-0.04em" }}>
              {u.t("lottery:arena_title")}
            </Typography>
            <Typography
              sx={{
                maxWidth: 620,
                mt: 1,
                color: theme.colors.alpha.trueWhite[70],
                fontSize: { xs: "1rem", md: "1.1rem" },
              }}
            >
              {u.t("lottery:arena_description")}
            </Typography>
          </Grid>
          <Grid
            size={{ xs: 12, md: 5 }}
            container
            spacing={1}
            sx={{ alignContent: "flex-end", justifyContent: { xs: "flex-start", md: "flex-end" } }}
          >
            <Grid size={{ xs: 4 }}>
              <Typography variant="h3" sx={{ color: heroTextColors.value, fontWeight: 900 }}>
                {stats.activeDraws}
              </Typography>
              <Typography variant="caption" sx={{ color: heroTextColors.label, fontWeight: 700 }}>
                {u.t("lottery:arena_active_draws")}
              </Typography>
            </Grid>
            <Grid size={{ xs: 4 }}>
              <Typography variant="h3" sx={{ color: heroTextColors.value, fontWeight: 900 }}>
                {stats.tickets}
              </Typography>
              <Typography variant="caption" sx={{ color: heroTextColors.label, fontWeight: 700 }}>
                {u.t("lottery:arena_tickets")}
              </Typography>
            </Grid>
            <Grid size={{ xs: 4 }}>
              <Typography
                variant="h3"
                sx={{ color: theme.colors.warning.main, fontWeight: 900 }}
              >
                {stats.wins}
              </Typography>
              <Typography variant="caption" sx={{ color: heroTextColors.label, fontWeight: 700 }}>
                {u.t("lottery:arena_wins")}
              </Typography>
            </Grid>
          </Grid>
        </Grid>
      </Card>

      <Grid container spacing={1.5} sx={{ mt: 1.5 }}>
        <Grid size={{ xs: 12, md: 8 }}>
          <Grid container spacing={1.5}>
            <Grid size={12}>
              <ActiveDraws onDrawsChange={setActiveDraws} />
            </Grid>
            <Grid size={12}>
              <HistoricalTickets SetLatestTickets={updateLatestTickets} />
            </Grid>
          </Grid>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <LatestTickets LatestTickets={latestTickets} />
        </Grid>
      </Grid>
    </Box>
  );
};

export default LotteryHomePage;
