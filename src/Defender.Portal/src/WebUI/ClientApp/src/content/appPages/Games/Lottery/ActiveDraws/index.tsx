import AutoAwesomeIcon from "@mui/icons-material/AutoAwesome";
import CachedIcon from "@mui/icons-material/Cached";
import CasinoIcon from "@mui/icons-material/Casino";
import { Box, Card, Chip, Grid, Typography } from "@mui/material";
import { useEffect, useRef, useState } from "react";
import { connect } from "react-redux";

import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import RequestParamsBuilder from "src/api/APIWrapper/RequestParamsBuilder";
import useUtils from "src/appUtils";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import { compactIconButtonLayout } from "src/components/Buttons/buttonLayouts";
import apiUrls from "src/api/apiUrls";
import { PaginationRequest } from "src/models/base/PaginationRequest";
import LotteryDraw from "src/models/games/lottery/LotteryDraw";
import ActiveLotteryDrawsResponse from "src/models/responses/games/lottery/ActiveLotteryDrawsResponse";
import DrawCard from "./DrawCard";

interface ActiveDrawsProps {
  onDrawsChange?: (draws: LotteryDraw[]) => void;
}

const ActiveDraws = (props: ActiveDrawsProps) => {
  const u = useUtils();
  const theme = u.react.theme;
  const reloadActiveDrawsRef = useRef<() => void>(() => undefined);
  const [draws, setDraws] = useState<LotteryDraw[]>([]);
  const [paginationRequest] = useState<PaginationRequest>({
    page: 0,
    pageSize: 1000,
  } as PaginationRequest);

  useEffect(() => {
    reloadActiveDrawsRef.current();
  }, [paginationRequest]);

  const reloadActiveDraws = () => {
    const url =
      `${apiUrls.lottery.getActiveDraws}` +
      `${RequestParamsBuilder.BuildQuery(paginationRequest)}`;

    APICallWrapper({
      url,
      options: {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      },
      utils: u,
      onSuccess: async (response) => {
        const activeDraws: ActiveLotteryDrawsResponse = await response.json();
        setDraws(activeDraws.items);
        props.onDrawsChange?.(activeDraws.items);
      },
      onFailure: async () => {},
      showError: true,
    });
  };
  reloadActiveDrawsRef.current = reloadActiveDraws;

  const renderDrawCard = (draw: LotteryDraw, index: number) => {
    return <DrawCard key={draw.drawNumber || index} draw={draw} reloadActiveDraws={reloadActiveDraws} />;
  };

  return (
    <Card
      sx={{
        p: { xs: 1.5, md: 2 },
        border: `1px solid ${theme.colors.primary.lighter}`,
        background: `radial-gradient(circle at 100% 0%, ${theme.colors.primary.lighter}, transparent 34%), ${theme.palette.background.paper}`,
      }}
    >
      <Grid container spacing={2} sx={{ alignItems: "center" }}>
        <Grid size={{ xs: 12, sm: 8 }}>
          <Grid container spacing={1} sx={{ alignItems: "center" }}>
            <Grid size={{ xs: 2, sm: 1 }}>
              <Box
                sx={{
                  width: 42,
                  height: 42,
                  borderRadius: 2,
                  display: "grid",
                  placeItems: "center",
                  color: theme.palette.primary.contrastText,
                  background: theme.colors.gradients.purple3,
                  boxShadow: theme.colors.shadows.primary,
                }}
              >
                <CasinoIcon />
              </Box>
            </Grid>
            <Grid size={{ xs: 10, sm: 11 }}>
              <Typography variant="overline" sx={{ color: theme.palette.primary.main, fontWeight: 800 }}>
                {u.t("lottery:arena_draws_kicker")}
              </Typography>
              <Typography variant="h4" sx={{ fontWeight: 900, lineHeight: 1.05 }}>
                {u.t("lottery:active_draws_title")}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {u.t("lottery:arena_draws_subtitle")}
              </Typography>
            </Grid>
          </Grid>
        </Grid>
        <Grid
          size={{ xs: 12, sm: 4 }}
          container
          spacing={1}
          sx={{ justifyContent: { xs: "flex-start", sm: "flex-end" }, alignItems: "center" }}
        >
          <Grid size={{ xs: 2, sm: 2 }}>
            <Chip
              icon={<AutoAwesomeIcon />}
              label={`${draws.length}`}
              size="small"
              sx={{
                color: theme.palette.primary.main,
                backgroundColor: theme.colors.primary.lighter,
                "& .MuiChip-icon": { color: theme.palette.primary.main },
              }}
            />
          </Grid>
          <Grid size={{ xs: 2, sm: 2 }}>
            <LockedButton
              aria-label="Refresh active draws"
              variant="outlined"
              onClick={reloadActiveDraws}
              sx={compactIconButtonLayout}
            >
              <CachedIcon />
            </LockedButton>
          </Grid>
        </Grid>
        <Grid size={12}>
          {draws.length > 0 ? (
            <Grid container spacing={1.5}>
              {draws.map(renderDrawCard)}
            </Grid>
          ) : (
            <Box
              sx={{
                minHeight: 150,
                px: 2,
                py: 3,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                textAlign: "center",
                borderRadius: theme.general.borderRadiusLg,
                border: `1px dashed ${theme.colors.primary.light}`,
                backgroundColor: theme.colors.primary.lighter,
              }}
            >
              <AutoAwesomeIcon sx={{ mb: 1, color: theme.palette.primary.main }} />
              <Typography variant="h6" sx={{ fontWeight: 800 }}>
                {u.t("lottery:arena_no_draws_title")}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {u.t("lottery:arena_no_draws_description")}
              </Typography>
            </Box>
          )}
        </Grid>
      </Grid>
    </Card>
  );
};

const mapStateToProps = (state: any) => {
  return {};
};

export default connect(mapStateToProps)(ActiveDraws);
