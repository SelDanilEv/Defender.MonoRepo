import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  LinearProgress,
  Typography,
} from "@mui/material";
import { useCallback, useEffect, useRef, useState } from "react";
import { connect } from "react-redux";
import { useNavigate, useParams } from "react-router-dom";

import useUtils from "src/appUtils";
import { foodAdviserApi, MenuSessionDto } from "src/api/foodAdviser";

const POLL_INTERVAL_MS = 5000;
const POLL_MAX_ATTEMPTS = 36;
const TOP_RECOMMENDATIONS = 3;

const FoodAdviserSessionRecommendationsPage = () => {
  const u = useUtils();
  const navigate = useNavigate();
  const { sessionId } = useParams<{ sessionId: string }>();
  const [session, setSession] = useState<MenuSessionDto | null>(null);
  const [recommendations, setRecommendations] = useState<string[]>([]);
  const [pollStopped, setPollStopped] = useState(false);
  const [loadingRecommendations, setLoadingRecommendations] = useState(false);
  const [ratingDish, setRatingDish] = useState<string | null>(null);
  const [ratingValue, setRatingValue] = useState<number>(3);
  const [submitting, setSubmitting] = useState(false);

  const loadSession = useCallback(() => {
    if (!sessionId) return;
    foodAdviserApi.getSession(sessionId, u).then((data) => {
      if (data) setSession(data);
    });
  }, [sessionId, u]);

  const loadRecommendations = useCallback(() => {
    if (!sessionId) return;
    setLoadingRecommendations(true);
    foodAdviserApi.getRecommendations(sessionId, u).then((data) => {
      if (data && data.length > 0) setRecommendations(data);
    }).finally(() => setLoadingRecommendations(false));
  }, [sessionId, u]);

  useEffect(() => {
    loadSession();
    loadRecommendations();
  }, [loadSession, loadRecommendations]);

  const loadRecommendationsRef = useRef(loadRecommendations);
  loadRecommendationsRef.current = loadRecommendations;
  const pollAttemptsRef = useRef(0);

  useEffect(() => {
    if (!sessionId || recommendations.length > 0 || pollStopped) return;
    pollAttemptsRef.current = 0;
    const id = setInterval(() => {
      pollAttemptsRef.current += 1;
      if (pollAttemptsRef.current >= POLL_MAX_ATTEMPTS) {
        clearInterval(id);
        setPollStopped(true);
        return;
      }
      loadRecommendationsRef.current();
    }, POLL_INTERVAL_MS);
    return () => clearInterval(id);
  }, [sessionId, recommendations.length, pollStopped]);

  const handleSubmitRating = () => {
    if (!ratingDish) return;
    setSubmitting(true);
    foodAdviserApi
      .submitRating(ratingDish, ratingValue, sessionId || null, u)
      .then(() => {
        setRatingDish(null);
        setRatingValue(3);
        setSubmitting(false);
      })
      .catch(() => setSubmitting(false));
  };

  const handleRefresh = () => {
    if (recommendations.length === 0) {
      setPollStopped(false);
    }
    loadRecommendations();
  };

  return (
    <Box>
      <Typography variant="h4" sx={{ mb: 2 }}>
        {u.t("foodAdviser:recommendations_title")}
      </Typography>
      {!!session && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          Session status: {session.status}
        </Typography>
      )}
      {loadingRecommendations && recommendations.length === 0 && (
        <LinearProgress sx={{ mb: 2 }} />
      )}
      <Grid container spacing={2}>
        {recommendations.length === 0 ? (
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center" gap={1}>
                  {!pollStopped && <CircularProgress size={16} />}
                  <Typography color="text.secondary">
                  {!sessionId
                    ? u.t("foodAdviser:recommendations_empty")
                    : pollStopped
                    ? u.t("foodAdviser:recommendations_timeout")
                    : u.t("foodAdviser:polling")}
                  </Typography>
                </Box>
                {pollStopped && (
                  <Alert severity="warning" sx={{ mt: 1.5 }}>
                    Background generation may still finish later. You can refresh and check again.
                  </Alert>
                )}
                <Box sx={{ mt: 1.5 }}>
                  <Button
                    variant="outlined"
                    size="small"
                    onClick={handleRefresh}
                    disabled={loadingRecommendations}
                  >
                    Refresh
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
                    {u.t("foodAdviser:i_picked_this")}
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
                  More suggestions
                </Typography>
                <Typography variant="body2">
                  {recommendations.slice(TOP_RECOMMENDATIONS).join(", ")}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        )}
        <Grid item xs={12}>
          <Button variant="outlined" onClick={() => navigate("/food-adviser")}>
            {u.t("foodAdviser:back")}
          </Button>
        </Grid>
      </Grid>

      <Dialog open={!!ratingDish} onClose={() => setRatingDish(null)}>
        <DialogTitle>{u.t("foodAdviser:rate_dish")}</DialogTitle>
        <DialogContent>
          {ratingDish && (
            <Typography variant="body2" gutterBottom>
              {ratingDish}
            </Typography>
          )}
          <Box display="flex" gap={1} alignItems="center" sx={{ mt: 1 }}>
            <Typography variant="body2">1</Typography>
            <Button
              size="small"
              variant={ratingValue === 1 ? "contained" : "outlined"}
              onClick={() => setRatingValue(1)}
            >
              1
            </Button>
            <Button
              size="small"
              variant={ratingValue === 2 ? "contained" : "outlined"}
              onClick={() => setRatingValue(2)}
            >
              2
            </Button>
            <Button
              size="small"
              variant={ratingValue === 3 ? "contained" : "outlined"}
              onClick={() => setRatingValue(3)}
            >
              3
            </Button>
            <Button
              size="small"
              variant={ratingValue === 4 ? "contained" : "outlined"}
              onClick={() => setRatingValue(4)}
            >
              4
            </Button>
            <Button
              size="small"
              variant={ratingValue === 5 ? "contained" : "outlined"}
              onClick={() => setRatingValue(5)}
            >
              5
            </Button>
            <Typography variant="body2">5</Typography>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRatingDish(null)}>
            {u.t("foodAdviser:back")}
          </Button>
          <Button
            variant="contained"
            onClick={handleSubmitRating}
            disabled={submitting}
          >
            {u.t("foodAdviser:rating_submit")}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

const mapStateToProps = () => ({});

export default connect(mapStateToProps)(FoodAdviserSessionRecommendationsPage);
