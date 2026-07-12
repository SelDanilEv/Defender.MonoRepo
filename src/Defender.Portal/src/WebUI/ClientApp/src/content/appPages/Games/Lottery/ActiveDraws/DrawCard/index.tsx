import { Card, Box, Typography, Grid } from "@mui/material";
import { reloadResources } from "i18next";
import dayjs from "dayjs";
import duration from "dayjs/plugin/duration";
import { useState, useEffect, useRef } from "react";
import { connect } from "react-redux";
import useUtils from "src/appUtils";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";
import LotteryDraw, { getDrawName } from "src/models/games/lottery/LotteryDraw";

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
  const utilsRef = useRef(u);
  utilsRef.current = u;
  const reloadActiveDrawsRef = useRef(reloadActiveDraws);
  reloadActiveDrawsRef.current = reloadActiveDraws;

  const [timeLeft, setTimeLeft] = useState<string>("");

  const [allowedToPlay, setAllowedToPlay] = useState<boolean>(false);

  const getSeconds = (time: string) => {
    const [hours, minutes, seconds] = time.split(":").map(Number);
    return hours * 60 * 60 + minutes * 60 + seconds;
  };

  useEffect(() => {
    const timer = setInterval(() => {
      const remainingMilliseconds = dayjs(draw.endDate).diff(dayjs());
      const daysLeft = dayjs(draw.endDate).diff(dayjs(), "days");
      const timeLeft =
        daysLeft > 0
          ? `${daysLeft} ${utilsRef.current.t("lottery:active_draws_days_left")}`
          : "";
      const hoursMinutesSecondsLeft = dayjs
        .duration(Math.max(remainingMilliseconds, 0))
        .format("HH:mm:ss");

      setAllowedToPlay(
        daysLeft > 0 ||
          getSeconds(hoursMinutesSecondsLeft) > getSeconds("00:05:00")
      );

      if (daysLeft <= 0 && hoursMinutesSecondsLeft === "00:00:00") {
        reloadActiveDrawsRef.current();
      }

      setTimeLeft(timeLeft + hoursMinutesSecondsLeft);
    }, 1000);
    return () => clearInterval(timer);
  }, [draw.endDate]);

  const handleDrawSelection = () => {
    u.react.navigate("/games/lottery/tickets", { state: { draw } });
  };

  return (
    <Card
      sx={{
        p: 0.5,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: 1,
      }}
    >
      <Box
        sx={{
          width: "95%",
          display: "flex",
          flexDirection: "row",
          justifyContent: "space-between"
        }}>
        <Card sx={{ width: "fit-content" }}>
          <Typography sx={{
            p: 0.7
          }}>{timeLeft}</Typography>
        </Card>
        <Card sx={{ width: "fit-content" }}>
          <Typography sx={{
            p: 0.7
          }}>{`#${draw.drawNumber}`}</Typography>
        </Card>
        <Box
          sx={{
            display: "flex",
            flexDirection: "row",
            gap: 1
          }}>
          {draw.allowedCurrencies.map((currency) => (
            <Card key={currency} sx={{ px: 1, height: "1.5em" }}>
              {CurrencySymbolsMap[currency]}
            </Card>
          ))}
        </Box>
      </Box>
      <Typography variant="h3">{getDrawName(draw, currentLanguage)}</Typography>
      <LockedButton
        disabled={!allowedToPlay}
        variant="outlined"
        onClick={handleDrawSelection}
      >
        {u.t("lottery:active_draws_play_from")}
        {draw.minBetValue / 100}
      </LockedButton>
    </Card>
  );
};

const mapStateToProps = (state: any) => {
  return {
    currentLanguage: state.session.language,
  };
};

export default connect(mapStateToProps)(DrawCard);
