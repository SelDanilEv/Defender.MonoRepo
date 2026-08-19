import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import AutoAwesomeIcon from "@mui/icons-material/AutoAwesome";
import ConfirmationNumberIcon from "@mui/icons-material/ConfirmationNumber";
import { Box, Card, Chip, Grid, Typography } from "@mui/material";
import { connect } from "react-redux";
import useUtils from "src/appUtils";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import LotteryDraw, { getDrawName } from "src/models/games/lottery/LotteryDraw";
import { getDrawMultiplier, readLotteryDrawState } from "../lotteryPresentation";
import SelectAndPayPanel from "./SelectAndPayPanel";

interface PurchaseTicketsProps {
  currentLanguage: string;
}

const PurchaseTickets = (props: PurchaseTicketsProps) => {
  const u = useUtils();
  const theme = u.react.theme;

  let drawState: unknown = null;
  try {
    drawState = u.react.locationState<unknown>("draw");
  } catch {
    drawState = null;
  }
  const draw = readLotteryDrawState(drawState);

  if (!draw) {
    return (
      <Card
        sx={{
          minHeight: "55vh",
          p: { xs: 2, md: 5 },
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          overflow: "hidden",
          background: `radial-gradient(circle at 50% 0%, ${theme.colors.primary.lighter}, transparent 42%), ${theme.palette.background.paper}`,
        }}
      >
        <Box sx={{ maxWidth: 560, textAlign: "center" }}>
          <Box
            sx={{
              width: 72,
              height: 72,
              mx: "auto",
              mb: 2,
              borderRadius: "50%",
              display: "grid",
              placeItems: "center",
              color: theme.palette.primary.contrastText,
              background: theme.colors.gradients.purple3,
              boxShadow: theme.colors.shadows.primary,
            }}
          >
            <ConfirmationNumberIcon fontSize="large" />
          </Box>
          <Typography variant="h3" sx={{ mb: 1, fontWeight: 800 }}>
            {u.t("lottery:draw_not_selected_title")}
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 3 }}>
            {u.t("lottery:draw_not_selected_description")}
          </Typography>
          <LockedButton
            variant="contained"
            startIcon={<ArrowBackIcon />}
            onClick={() => u.react.navigate("/games/lottery")}
          >
            {u.t("lottery:draw_not_selected_back_button")}
          </LockedButton>
        </Box>
      </Card>
    );
  }

  return (
    <Grid container spacing={1.5} sx={{ maxWidth: 1480, mx: "auto", pb: 2 }}>
      <Grid size={12}>
        <Card
          sx={{
            p: { xs: 2, md: 3 },
            position: "relative",
            overflow: "hidden",
            color: theme.colors.alpha.trueWhite[100],
            background: `radial-gradient(circle at 84% 18%, ${theme.colors.info.main}44, transparent 28%), radial-gradient(circle at 12% 100%, ${theme.colors.primary.main}66, transparent 34%), ${theme.colors.gradients.blue3}`,
            boxShadow: theme.colors.shadows.primary,
            "&::after": {
              content: '""',
              position: "absolute",
              width: 180,
              height: 180,
              right: -70,
              bottom: -90,
              borderRadius: "50%",
              border: `1px solid ${theme.colors.alpha.trueWhite[30]}`,
              boxShadow: `0 0 0 20px ${theme.colors.alpha.trueWhite[5]}, 0 0 0 40px ${theme.colors.alpha.trueWhite[5]}`,
            },
          }}
        >
          <Grid container spacing={2} sx={{ position: "relative", zIndex: 1 }}>
            <Grid size={{ xs: 12, md: 8 }}>
              <Chip
                icon={<AutoAwesomeIcon />}
                label={u.t("lottery:draw_entry_kicker")}
                sx={{
                  mb: 1.5,
                  color: theme.colors.alpha.trueWhite[100],
                  backgroundColor: theme.colors.alpha.trueWhite[10],
                  border: `1px solid ${theme.colors.alpha.trueWhite[30]}`,
                  "& .MuiChip-icon": { color: theme.colors.warning.main },
                }}
              />
              <Typography variant="h2" sx={{ fontWeight: 900, letterSpacing: "-0.03em" }}>
                {getDrawName(draw, props.currentLanguage)}
              </Typography>
              <Typography sx={{ mt: 0.75, color: theme.colors.alpha.trueWhite[70] }}>
                {u.t("lottery:draw_entry_subtitle")}
              </Typography>
            </Grid>
            <Grid
              size={{ xs: 12, md: 4 }}
              container
              spacing={1}
              sx={{ alignContent: "center", justifyContent: { xs: "flex-start", md: "flex-end" } }}
            >
              <Grid size={{ xs: 6, md: 12 }}>
                <Typography variant="overline" sx={{ color: theme.colors.alpha.trueWhite[50] }}>
                  {u.t("lottery:draw_entry_draw_number")}
                </Typography>
                <Typography variant="h4" sx={{ fontWeight: 800 }}>
                  #{draw.drawNumber}
                </Typography>
              </Grid>
              <Grid size={{ xs: 6, md: 12 }}>
                <Typography variant="overline" sx={{ color: theme.colors.alpha.trueWhite[50] }}>
                  {u.t("lottery:draw_entry_multiplier")}
                </Typography>
                <Typography variant="h4" sx={{ color: theme.colors.warning.main, fontWeight: 900 }}>
                  {getDrawMultiplier(draw)}
                </Typography>
              </Grid>
            </Grid>
          </Grid>
        </Card>
      </Grid>
      <Grid size={12}>
        <SelectAndPayPanel draw={draw} />
      </Grid>
    </Grid>
  );
};

const mapStateToProps = (state: any) => {
  return {
    currentLanguage: state.session.language,
  };
};

export default connect(mapStateToProps)(PurchaseTickets);
