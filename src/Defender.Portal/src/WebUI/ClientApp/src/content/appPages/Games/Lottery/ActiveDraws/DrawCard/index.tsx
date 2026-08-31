import AccessTimeIcon from "@mui/icons-material/AccessTime";
import BoltIcon from "@mui/icons-material/Bolt";
import ConfirmationNumberIcon from "@mui/icons-material/ConfirmationNumber";
import { Box, Card, Chip, Grid, Typography } from "@mui/material";
import dayjs from "dayjs";
import duration from "dayjs/plugin/duration";
import { useEffect, useRef, useState } from "react";
import { connect } from "react-redux";

import useUtils from "src/appUtils";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";
import LotteryDraw, { getDrawName } from "src/models/games/lottery/LotteryDraw";
import { getDrawMultiplier } from "../../lotteryPresentation";

dayjs.extend(duration);

interface DrawCardProps {
  draw: LotteryDraw;
  currentLanguage: string;
  reloadActiveDraws: () => void;
}

const DrawCard: React.FC<DrawCardProps> = ({
  reloadActiveDraws,
  draw,
  currentLanguage,
}) => {
  const u = useUtils();
  const theme = u.react.theme;
  const utilsRef = useRef(u);
  utilsRef.current = u;
  const reloadActiveDrawsRef = useRef(reloadActiveDraws);
  reloadActiveDrawsRef.current = reloadActiveDraws;
  const [timeLeft, setTimeLeft] = useState<string>("—");
  const [allowedToPlay, setAllowedToPlay] = useState<boolean>(false);

  const getSeconds = (time: string) => {
    const [hours, minutes, seconds] = time.split(":").map(Number);
    return hours * 60 * 60 + minutes * 60 + seconds;
  };

  useEffect(() => {
    const updateTimer = () => {
      const remainingMilliseconds = dayjs(draw.endDate).diff(dayjs());
      const daysLeft = dayjs(draw.endDate).diff(dayjs(), "days");
      const hoursMinutesSecondsLeft = dayjs
        .duration(Math.max(remainingMilliseconds, 0))
        .format("HH:mm:ss");
      const formattedTimeLeft =
        daysLeft > 0
          ? `${daysLeft} ${utilsRef.current.t("lottery:active_draws_days_left")}`
          : hoursMinutesSecondsLeft;

      setAllowedToPlay(
        daysLeft > 0 || getSeconds(hoursMinutesSecondsLeft) > getSeconds("00:05:00"),
      );

      if (daysLeft <= 0 && hoursMinutesSecondsLeft === "00:00:00") {
        reloadActiveDrawsRef.current();
      }

      setTimeLeft(formattedTimeLeft);
    };

    updateTimer();
    const timer = setInterval(updateTimer, 1000);
    return () => clearInterval(timer);
  }, [draw.endDate]);

  const handleDrawSelection = () => {
    u.react.navigate("/games/lottery/tickets", { state: { draw } });
  };

  return (
    <Card
      data-testid="lottery-active-draw-card"
      sx={{
        height: "100%",
        width: "100%",
        p: { xs: 1.5, md: 2 },
        position: "relative",
        overflow: "hidden",
        border: `1px solid ${theme.colors.primary.light}`,
        background: `radial-gradient(circle at 92% 8%, ${theme.colors.info.lighter}, transparent 30%), ${theme.palette.background.paper}`,
        transition: "transform 180ms ease, box-shadow 180ms ease, border-color 180ms ease",
        "&:hover": {
          transform: "translateY(-3px)",
          borderColor: theme.colors.primary.main,
          boxShadow: theme.colors.shadows.primary,
        },
        "@media (prefers-reduced-motion: reduce)": {
          transition: "none",
          "&:hover": { transform: "none" },
        },
      }}
    >
      <Grid container spacing={1.5} sx={{ position: "relative", zIndex: 1 }}>
        <Grid size={{ xs: 12 }} container sx={{ justifyContent: "space-between", alignItems: "center" }}>
          <Chip
            icon={<BoltIcon />}
            label={`#${draw.drawNumber}`}
            size="small"
            sx={{
              color: theme.palette.primary.main,
              backgroundColor: theme.colors.primary.lighter,
              fontWeight: 800,
              "& .MuiChip-icon": { color: theme.palette.primary.main },
            }}
          />
          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 0.5,
              px: 1,
              py: 0.5,
              borderRadius: theme.general.borderRadius,
              color: theme.palette.warning.dark,
              backgroundColor: theme.colors.warning.lighter,
            }}
          >
            <AccessTimeIcon sx={{ fontSize: 17 }} />
            <Typography variant="caption" sx={{ fontWeight: 800 }}>
              {timeLeft}
            </Typography>
          </Box>
        </Grid>
        <Grid size={{ xs: 12 }}>
          <Typography variant="h4" sx={{ fontWeight: 900, lineHeight: 1.1 }}>
            {getDrawName(draw, currentLanguage)}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {u.t("lottery:arena_draw_card_currencies")}
          </Typography>
        </Grid>
        <Grid size={{ xs: 7 }}>
          <Typography variant="overline" color="text.secondary">
            {u.t("lottery:arena_draw_card_multiplier")}
          </Typography>
          <Typography variant="h3" sx={{ fontWeight: 900, color: theme.palette.warning.main }}>
            {getDrawMultiplier(draw)}
          </Typography>
        </Grid>
        <Grid size={{ xs: 5 }} container spacing={0.5} sx={{ justifyContent: "flex-end", alignItems: "center" }}>
          {draw.allowedCurrencies.map((currency) => (
            <Grid key={currency} size={{ xs: 6 }}>
              <Box
                data-testid="lottery-currency-option"
                sx={{
                  minWidth: 30,
                  py: 0.5,
                  textAlign: "center",
                  borderRadius: theme.general.borderRadiusSm,
                  color: theme.palette.info.main,
                  backgroundColor: theme.colors.info.lighter,
                  fontWeight: 800,
                }}
              >
                {CurrencySymbolsMap[currency]}
              </Box>
            </Grid>
          ))}
        </Grid>
        <Grid size={{ xs: 12 }}>
          <LockedButton
            fullWidth
            disabled={!allowedToPlay}
            variant="contained"
            startIcon={<ConfirmationNumberIcon />}
            onClick={handleDrawSelection}
            sx={{
              minHeight: 42,
              fontWeight: 800,
              boxShadow: theme.colors.shadows.primary,
            }}
          >
            {u.t("lottery:arena_draw_card_action")} · {draw.minBetValue / 100}
          </LockedButton>
        </Grid>
      </Grid>
    </Card>
  );
};

const mapStateToProps = (state: any) => {
  return {
    currentLanguage: state.session.language,
  };
};

export default connect(mapStateToProps)(DrawCard);
