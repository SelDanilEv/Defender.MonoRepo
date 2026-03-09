import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  LinearProgress,
  Typography,
} from "@mui/material";
import { useCallback, useEffect, useRef, useState } from "react";
import { connect } from "react-redux";
import { useNavigate, useParams } from "react-router-dom";

import useUtils from "src/appUtils";
import { foodAdvisorApi, MenuSessionDto } from "src/api/foodAdvisor";
import DishRatingDialog from "src/components/DishRatingDialog";

const TOP_RECOMMENDATIONS = 3;
const AUTO_POLL_INTERVAL_MS = 15_000;
const AUTO_POLL_WINDOW_MS = 3 * 60 * 1000;

const FoodAdvisorSessionRecommendationsPage = () => {
  const u = useUtils();
  const navigate = useNavigate();
  const { sessionId } = useParams<{ sessionId: string }>();
  const [session, setSession] = useState<MenuSessionDto | null>(null);
  const [recommendations, setRecommendations] = useState<string[]>([]);
  const [loadingRecommendations, setLoadingRecommendations] = useState(false);
  const [manualRefreshWarning, setManualRefreshWarning] = useState<string | null>(null);
  const [ratingDish, setRatingDish] = useState<string | null>(null);
  const [ratingValue, setRatingValue] = useState<number>(3);
  const [submitting, setSubmitting] = useState(false);
  const [autoPollingStartedAt, setAutoPollingStartedAt] = useState<number | null>(null);
  const utilsRef = useRef(u);
  const pollingStartedAtRef = useRef<number | null>(null);
  const pollingInFlightRef = useRef(false);
  utilsRef.current = u;

  const getStatusLabel = useCallback((status: string | null | undefined) => {
    if (!status) {
      return u.t("foodAdvisor:session_pending_creation");
    }

    const statusKey = `foodAdvisor:status_${status.toLowerCase()}`;
    const localizedStatus = u.t(statusKey);

    return localizedStatus === statusKey ? status : localizedStatus;
  }, [u]);

  const startAutoPolling = useCallback(() => {
    const startedAt = Date.now();
    pollingStartedAtRef.current = startedAt;
    setAutoPollingStartedAt(startedAt);
  }, []);

  const stopAutoPolling = useCallback(() => {
    pollingStartedAtRef.current = null;
    setAutoPollingStartedAt(null);
  }, []);

  const loadSession = useCallback(() => {
    if (!sessionId) return;
    return foodAdvisorApi.getSession(sessionId, utilsRef.current).then((data) => {
      if (!data) return;

      setSession(data);
      if (data.recommendationWarningMessage) {
        setManualRefreshWarning(data.recommendationWarningMessage);
        return;
      }

      setManualRefreshWarning(null);
    });
  }, [sessionId]);

  const loadRecommendations = useCallback((options?: { silent?: boolean }) => {
    if (!sessionId) return;

    if (!options?.silent) {
      setLoadingRecommendations(true);
    }

    return foodAdvisorApi.getRecommendations(sessionId, utilsRef.current)
      .then((data) => {
        setRecommendations(data ?? []);
      })
      .finally(() => {
        if (!options?.silent) {
          setLoadingRecommendations(false);
        }
      });
  }, [sessionId]);

  useEffect(() => {
    if (!sessionId) {
      return;
    }

    startAutoPolling();
    loadSession();
    loadRecommendations();
  }, [loadSession, loadRecommendations, sessionId, startAutoPolling]);

  useEffect(() => {
    if (!sessionId || !autoPollingStartedAt) {
      return;
    }

    if (manualRefreshWarning || recommendations.length > 0) {
      stopAutoPolling();
      return;
    }

    const intervalId = window.setInterval(() => {
      const startedAt = pollingStartedAtRef.current;

      if (!startedAt || Date.now() - startedAt >= AUTO_POLL_WINDOW_MS) {
        stopAutoPolling();
        return;
      }

      if (pollingInFlightRef.current) {
        return;
      }

      pollingInFlightRef.current = true;
      Promise.all([
        loadSession(),
        loadRecommendations({ silent: true }),
      ]).finally(() => {
        pollingInFlightRef.current = false;

        const nextStartedAt = pollingStartedAtRef.current;
        if (nextStartedAt && Date.now() - nextStartedAt >= AUTO_POLL_WINDOW_MS) {
          stopAutoPolling();
        }
      });
    }, AUTO_POLL_INTERVAL_MS);

    return () => window.clearInterval(intervalId);
  }, [
    autoPollingStartedAt,
    loadRecommendations,
    loadSession,
    manualRefreshWarning,
    recommendations.length,
    sessionId,
    stopAutoPolling,
  ]);

  const handleSubmitRating = () => {
    if (!ratingDish) return;
    setSubmitting(true);
    foodAdvisorApi
      .submitRating(ratingDish, ratingValue, sessionId || null, u)
      .then(() => {
        setRatingDish(null);
        setRatingValue(3);
        setSubmitting(false);
      })
      .catch(() => setSubmitting(false));
  };

  const handleRefresh = () => {
    if (!sessionId) {
      return;
    }

    if (manualRefreshWarning) {
      setManualRefreshWarning(null);
      setRecommendations([]);

      foodAdvisorApi
        .requestRecommendations(sessionId, utilsRef.current)
        .then(() => {
          startAutoPolling();
          loadSession();
          loadRecommendations();
        })
        .catch(() => {
          setManualRefreshWarning(manualRefreshWarning);
        });
      return;
    }

    startAutoPolling();
    loadSession();
    loadRecommendations();
  };

  return (
    <Box sx={{ pt: 1 }}>
      <Box sx={{ mb: 2.5, textAlign: "center" }}>
        <Typography variant="h4" sx={{ mb: 1 }}>
          {u.t("foodAdvisor:recommendations_title")}
        </Typography>
        {!!session && (
          <Typography
            variant="body2"
            color="text.secondary"
            sx={{ maxWidth: 560, mx: "auto" }}
          >
            {u.t("foodAdvisor:session_status")}: {getStatusLabel(session.status)}
          </Typography>
        )}
      </Box>
      {loadingRecommendations && recommendations.length === 0 && (
        <LinearProgress sx={{ mb: 2 }} />
      )}
      <Grid container spacing={2}>
        {recommendations.length === 0 ? (
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography color="text.secondary">
                  {!sessionId
                    ? u.t("foodAdvisor:recommendations_empty")
                    : manualRefreshWarning
                    ? manualRefreshWarning
                    : u.t("foodAdvisor:recommendations_pending_manual")}
                </Typography>
                {manualRefreshWarning && (
                  <Alert severity="warning" sx={{ mt: 1.5 }}>
                    {u.t("foodAdvisor:recommendations_manual_refresh_hint")}
                  </Alert>
                )}
                {!manualRefreshWarning && sessionId && (
                  <Alert severity="info" sx={{ mt: 1.5 }}>
                    {autoPollingStartedAt
                      ? u.t("foodAdvisor:recommendations_auto_refresh_hint")
                      : u.t("foodAdvisor:recommendations_manual_only_hint")}
                  </Alert>
                )}
                <Box sx={{ mt: 1.5 }}>
                  <Button
                    variant="outlined"
                    size="small"
                    onClick={handleRefresh}
                    disabled={loadingRecommendations}
                  >
                    {u.t("foodAdvisor:recommendations_refresh")}
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ) : (
          recommendations.slice(0, TOP_RECOMMENDATIONS).map((dish, index) => (
            <Grid item xs={12} sm={6} md={4} key={index}>
              <Card
                sx={{
                  height: "100%",
                  display: "flex",
                  flexDirection: "column",
                }}
              >
                <CardContent
                  sx={{
                    flex: 1,
                    display: "flex",
                    flexDirection: "column",
                    justifyContent: "space-between",
                  }}
                >
                  <Typography variant="subtitle1" gutterBottom>
                    {index + 1}. {dish}
                  </Typography>
                  <Button
                    size="small"
                    variant="outlined"
                    fullWidth
                    onClick={() => setRatingDish(dish)}
                    sx={{ mt: 1 }}
                  >
                    {u.t("foodAdvisor:i_picked_this")}
                  </Button>
                </CardContent>
              </Card>
            </Grid>
          ))
        )}
        {recommendations.length > TOP_RECOMMENDATIONS && (
          <Grid item xs={12}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  {u.t("foodAdvisor:recommendations_more")}
                </Typography>
                <Typography variant="body2">
                  {recommendations.slice(TOP_RECOMMENDATIONS).join(", ")}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        )}
        <Grid item xs={12}>
          <Button variant="outlined" onClick={() => navigate("/food-advisor")}>
            {u.t("foodAdvisor:back")}
          </Button>
        </Grid>
      </Grid>

      <DishRatingDialog
        open={!!ratingDish}
        dishName={ratingDish}
        ratingValue={ratingValue}
        submitting={submitting}
        title={u.t("foodAdvisor:rate_dish")}
        submitLabel={u.t("foodAdvisor:rating_submit")}
        cancelLabel={u.t("foodAdvisor:back")}
        lowHint={u.t("foodAdvisor:rating_1")}
        highHint={u.t("foodAdvisor:rating_5")}
        onChange={setRatingValue}
        onClose={() => setRatingDish(null)}
        onSubmit={handleSubmitRating}
      />
    </Box>
  );
};

const mapStateToProps = () => ({});

export default connect(mapStateToProps)(FoodAdvisorSessionRecommendationsPage);
