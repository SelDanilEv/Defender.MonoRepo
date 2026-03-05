import ExpandLessIcon from "@mui/icons-material/ExpandLess";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import {
  Box,
  Button,
  Card,
  CardContent,
  Collapse,
  Divider,
  Grid,
  LinearProgress,
  Stack,
  TablePagination,
  Typography,
} from "@mui/material";
import { useEffect, useMemo, useRef, useState } from "react";
import type { ChangeEvent } from "react";
import { connect } from "react-redux";
import { useNavigate } from "react-router-dom";

import useUtils from "src/appUtils";
import {
  DishRatingDto,
  foodAdviserApi,
  MenuSessionDto,
} from "src/api/foodAdviser";
import CustomDialog from "src/components/Dialog";
import DishRatingDialog from "src/components/DishRatingDialog";
import TagChip from "src/components/TagChip";

const normalizeDishName = (dishName: string) => dishName.trim().toLowerCase();

const getSessionDishes = (session: MenuSessionDto) =>
  Array.from(
    new Set(
      [...session.confirmedItems, ...session.rankedItems, ...session.parsedItems]
        .map((dish) => dish.trim())
        .filter(Boolean)
    )
  );

const formatDate = (value: string | null) => {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
};

const getSessionActionPath = (session: MenuSessionDto) => {
  const status = session.status.toLowerCase();
  if (status === "confirmed" || session.rankedItems.length > 0) {
    return `/food-adviser/session/${session.id}/recommendations`;
  }

  return `/food-adviser/session/${session.id}/review`;
};

const FoodAdviserSessionsPage = () => {
  const u = useUtils();
  const navigate = useNavigate();
  const utilsRef = useRef(u);
  utilsRef.current = u;
  const [sessions, setSessions] = useState<MenuSessionDto[]>([]);
  const [ratings, setRatings] = useState<DishRatingDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(5);
  const [submitting, setSubmitting] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<MenuSessionDto | null>(null);
  const [deletingSessionId, setDeletingSessionId] = useState<string | null>(null);
  const [expandedSessionIds, setExpandedSessionIds] = useState<string[]>([]);
  const [ratingTarget, setRatingTarget] = useState<{
    dishName: string;
    sessionId: string;
    ratingValue: number;
  } | null>(null);

  useEffect(() => {
    setLoading(true);
    Promise.all([
      foodAdviserApi.getSessions(utilsRef.current),
      foodAdviserApi.getRatings(utilsRef.current),
    ])
      .then(([loadedSessions, loadedRatings]) => {
        setSessions(loadedSessions);
        setRatings(loadedRatings);
      })
      .finally(() => setLoading(false));
  }, []);

  const ratingsByDish = useMemo(() => {
    return ratings.reduce<Record<string, DishRatingDto>>((acc, rating) => {
      acc[normalizeDishName(rating.dishName)] = rating;
      return acc;
    }, {});
  }, [ratings]);

  const pagedSessions = useMemo(() => {
    const startIndex = page * rowsPerPage;
    return sessions.slice(startIndex, startIndex + rowsPerPage);
  }, [sessions, page, rowsPerPage]);

  useEffect(() => {
    const maxPage = sessions.length === 0
      ? 0
      : Math.max(0, Math.ceil(sessions.length / rowsPerPage) - 1);

    setPage((currentPage) => Math.min(currentPage, maxPage));
  }, [sessions.length, rowsPerPage]);

  const handleOpenRating = (sessionId: string, dishName: string) => {
    const existingRating = ratingsByDish[normalizeDishName(dishName)];
    setRatingTarget({
      sessionId,
      dishName,
      ratingValue: existingRating?.rating ?? 3,
    });
  };

  const handleConfirmDelete = () => {
    if (!deleteTarget || deletingSessionId) {
      return;
    }

    const sessionId = deleteTarget.id;
    setDeletingSessionId(sessionId);
    foodAdviserApi
      .deleteSession(sessionId, utilsRef.current)
      .then(() => {
        setSessions((prev) => prev.filter((session) => session.id !== sessionId));
        setRatings((prev) => prev.filter((rating) => rating.sessionId !== sessionId));
        setExpandedSessionIds((prev) => prev.filter((id) => id !== sessionId));
        setDeleteTarget((prev) => (prev?.id === sessionId ? null : prev));
        setRatingTarget((prev) => (prev?.sessionId === sessionId ? null : prev));
      })
      .finally(() => setDeletingSessionId(null));
  };

  const handleChangePage = (_event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (
    event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleToggleSessionDetails = (sessionId: string) => {
    setExpandedSessionIds((prev) => (
      prev.includes(sessionId)
        ? prev.filter((id) => id !== sessionId)
        : [...prev, sessionId]
    ));
  };

  const handleSubmitRating = () => {
    if (!ratingTarget) {
      return;
    }

    setSubmitting(true);
    foodAdviserApi
      .submitRating(
        ratingTarget.dishName,
        ratingTarget.ratingValue,
        ratingTarget.sessionId,
        utilsRef.current
      )
      .then(() => {
        setRatings((prev) => {
          const next = [...prev];
          const normalizedDishName = normalizeDishName(ratingTarget.dishName);
          const existingIndex = next.findIndex(
            (rating) => normalizeDishName(rating.dishName) === normalizedDishName
          );

          const nextRating: DishRatingDto = existingIndex >= 0
            ? {
                ...next[existingIndex],
                dishName: ratingTarget.dishName,
                rating: ratingTarget.ratingValue,
                sessionId: ratingTarget.sessionId,
                updatedAtUtc: new Date().toISOString(),
              }
            : {
                id: `${ratingTarget.sessionId}:${normalizedDishName}`,
                userId: sessions[0]?.userId ?? "",
                dishName: ratingTarget.dishName,
                rating: ratingTarget.ratingValue,
                sessionId: ratingTarget.sessionId,
                createdAtUtc: new Date().toISOString(),
                updatedAtUtc: null,
              };

          if (existingIndex >= 0) {
            next[existingIndex] = nextRating;
            return next;
          }

          return [nextRating, ...next];
        });
        setRatingTarget(null);
      })
      .finally(() => setSubmitting(false));
  };

  return (
    <Box sx={{ pt: 1 }}>
      <Box sx={{ mb: 2.5, textAlign: "center" }}>
        <Typography variant="h4" sx={{ mb: 1 }}>
          {u.t("foodAdviser:sessions_title")}
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ maxWidth: 560, mx: "auto" }}
        >
          {u.t("foodAdviser:sessions_subtitle")}
        </Typography>
      </Box>
      <Box display="flex" gap={1} flexWrap="wrap" sx={{ mb: 2 }}>
        <Button variant="contained" onClick={() => navigate("/food-adviser/session/new")}>
          {u.t("foodAdviser:new_session")}
        </Button>
        <Button variant="outlined" onClick={() => navigate("/food-adviser")}>
          {u.t("foodAdviser:back")}
        </Button>
      </Box>
      {loading && <LinearProgress sx={{ mb: 2 }} />}
      <Grid container spacing={2}>
        {!loading && sessions.length === 0 && (
          <Grid item xs={12}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="body1" gutterBottom>
                  {u.t("foodAdviser:sessions_empty")}
                </Typography>
                <Button variant="contained" onClick={() => navigate("/food-adviser/session/new")}>
                  {u.t("foodAdviser:new_session")}
                </Button>
              </CardContent>
            </Card>
          </Grid>
        )}
        {pagedSessions.map((session) => {
          const sessionDishes = getSessionDishes(session);
          const sessionStatusKey = `foodAdviser:status_${session.status.toLowerCase()}`;
          const detailsExpanded = expandedSessionIds.includes(session.id);

          return (
            <Grid item xs={12} key={session.id}>
              <Card variant="outlined">
                <CardContent>
                  <Stack
                    direction={{ xs: "column", md: "row" }}
                    spacing={2}
                    justifyContent="space-between"
                    sx={{ mb: 2 }}
                  >
                    <Box>
                      <Typography variant="h6">
                        {u.t("foodAdviser:sessions_session_label")}
                      </Typography>
                    </Box>
                    <Box display="flex" gap={1} flexWrap="wrap" alignItems="flex-start">
                      <TagChip label={u.t(sessionStatusKey)} />
                      <Button
                        variant="outlined"
                        size="small"
                        disabled={deletingSessionId === session.id}
                        onClick={() => navigate(getSessionActionPath(session))}
                      >
                        {u.t("foodAdviser:sessions_open")}
                      </Button>
                      <Button
                        variant="text"
                        size="small"
                        endIcon={detailsExpanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                        onClick={() => handleToggleSessionDetails(session.id)}
                      >
                        {detailsExpanded
                          ? u.t("foodAdviser:sessions_hide_dishes")
                          : u.t("foodAdviser:sessions_show_dishes")}
                      </Button>
                      <Button
                        variant="outlined"
                        color="error"
                        size="small"
                        disabled={deletingSessionId === session.id}
                        onClick={() => setDeleteTarget(session)}
                      >
                        {deletingSessionId === session.id
                          ? u.t("foodAdviser:polling")
                          : u.t("foodAdviser:sessions_delete")}
                      </Button>
                    </Box>
                  </Stack>

                  <Grid container spacing={2} sx={{ mb: 2 }}>
                    <Grid item xs={12} sm={6} md={4}>
                      <Typography variant="caption" color="text.secondary">
                        {u.t("foodAdviser:sessions_created")}
                      </Typography>
                      <Typography variant="body2">{formatDate(session.createdAtUtc)}</Typography>
                    </Grid>
                    <Grid item xs={12} sm={6} md={4}>
                      <Typography variant="caption" color="text.secondary">
                        {u.t("foodAdviser:sessions_updated")}
                      </Typography>
                      <Typography variant="body2">{formatDate(session.updatedAtUtc)}</Typography>
                    </Grid>
                    <Grid item xs={12} sm={6} md={4}>
                      <Typography variant="caption" color="text.secondary">
                        {u.t("foodAdviser:try_something_new")}
                      </Typography>
                      <Typography variant="body2">
                        {session.trySomethingNew
                          ? u.t("foodAdviser:sessions_yes")
                          : u.t("foodAdviser:sessions_no")}
                      </Typography>
                    </Grid>
                  </Grid>

                  <Divider sx={{ mb: 2 }} />

                  <Collapse in={detailsExpanded} timeout="auto" unmountOnExit>
                    <Grid container spacing={2}>
                      <Grid item xs={12}>
                        <Typography variant="subtitle2" sx={{ mb: 1 }}>
                          {u.t("foodAdviser:sessions_items")}
                        </Typography>
                        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                          {session.confirmedItems.length > 0 ? session.confirmedItems.map((item) => (
                            <TagChip key={`${session.id}-confirmed-${item}`} tone="positive" label={item} />
                          )) : (
                            <Typography variant="body2" color="text.secondary">
                              {u.t("foodAdviser:sessions_no_items")}
                            </Typography>
                          )}
                        </Stack>
                      </Grid>
                      <Grid item xs={12}>
                        <Typography variant="subtitle2" sx={{ mb: 1 }}>
                          {u.t("foodAdviser:sessions_ranked")}
                        </Typography>
                        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                          {session.rankedItems.length > 0 ? session.rankedItems
                            .map((item) => item.trim())
                            .filter(Boolean)
                            .map((item, index) => (
                              <TagChip
                                key={`${session.id}-ranked-${item}-${index}`}
                                tone="neutral"
                                label={`${index + 1}. ${item}`}
                              />
                            )) : (
                              <Typography variant="body2" color="text.secondary">
                                {u.t("foodAdviser:sessions_no_items")}
                              </Typography>
                            )}
                        </Stack>
                      </Grid>
                      <Grid item xs={12}>
                        <Typography variant="subtitle2" sx={{ mb: 1 }}>
                          {u.t("foodAdviser:sessions_dish_ratings")}
                        </Typography>
                        {sessionDishes.length > 0 ? (
                          <Stack spacing={1}>
                            {sessionDishes.map((dishName) => {
                              const rating = ratingsByDish[normalizeDishName(dishName)];

                              return (
                                <Box
                                  key={`${session.id}-rating-${dishName}`}
                                  display="flex"
                                  justifyContent="space-between"
                                  gap={2}
                                  alignItems={{ xs: "flex-start", md: "center" }}
                                  flexDirection={{ xs: "column", md: "row" }}
                                  sx={{
                                    p: 1.5,
                                    border: (theme) => `1px solid ${theme.palette.divider}`,
                                    borderRadius: 2,
                                  }}
                                >
                                  <Box>
                                    <Typography variant="body1" sx={{ fontWeight: 600 }}>
                                      {dishName}
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary">
                                      {rating
                                        ? `${u.t("foodAdviser:sessions_current_rating")}: ${rating.rating}/5`
                                        : u.t("foodAdviser:sessions_not_rated")}
                                    </Typography>
                                  </Box>
                                  <Box display="flex" gap={1} alignItems="center" flexWrap="wrap">
                                    {rating && (
                                      <TagChip
                                        label={`${rating.rating}/5`}
                                        tone={rating.rating >= 4 ? "positive" : "neutral"}
                                      />
                                    )}
                                    <Button
                                      variant="outlined"
                                      size="small"
                                      onClick={() => handleOpenRating(session.id, dishName)}
                                    >
                                      {rating
                                        ? u.t("foodAdviser:sessions_edit_rating")
                                        : u.t("foodAdviser:sessions_rate")}
                                    </Button>
                                  </Box>
                                </Box>
                              );
                            })}
                          </Stack>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            {u.t("foodAdviser:sessions_no_items")}
                          </Typography>
                        )}
                      </Grid>
                    </Grid>
                  </Collapse>
                </CardContent>
              </Card>
            </Grid>
          );
        })}
      </Grid>
      {!loading && sessions.length > 0 && (
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            justifyContent: "flex-end",
            flexWrap: "wrap",
            gap: 0.5,
            px: 1.5,
            pt: 0.5,
            pb: 0.75,
          }}
        >
          <Typography
            variant="body2"
            sx={{
              textAlign: "center",
              fontSize: { xs: "0.68rem", sm: "0.8rem" },
              lineHeight: 1.2,
            }}
          >
            {u.t("table_rows_per_page_label")}
          </Typography>
          <TablePagination
            component="div"
            count={sessions.length}
            page={page}
            onPageChange={handleChangePage}
            rowsPerPage={rowsPerPage}
            onRowsPerPageChange={handleChangeRowsPerPage}
            rowsPerPageOptions={[5, 10, 20]}
            labelRowsPerPage=""
          />
        </Box>
      )}

      <DishRatingDialog
        open={!!ratingTarget}
        dishName={ratingTarget?.dishName ?? null}
        ratingValue={ratingTarget?.ratingValue ?? 3}
        submitting={submitting}
        title={u.t("foodAdviser:rate_dish")}
        submitLabel={u.t("foodAdviser:rating_submit")}
        cancelLabel={u.t("foodAdviser:back")}
        lowHint={u.t("foodAdviser:rating_1")}
        highHint={u.t("foodAdviser:rating_5")}
        onChange={(value) =>
          setRatingTarget((prev) => (prev ? { ...prev, ratingValue: value } : prev))
        }
        onClose={() => setRatingTarget(null)}
        onSubmit={handleSubmitRating}
      />

      <CustomDialog
        title={u.t("foodAdviser:sessions_delete_title")}
        open={!!deleteTarget}
        onClose={() => {
          if (!deletingSessionId) {
            setDeleteTarget(null);
          }
        }}
      >
        <Stack spacing={2.5} sx={{ minWidth: { xs: 280, sm: 420 } }}>
          <Typography variant="subtitle1">
            {u.t("foodAdviser:sessions_delete_confirm")}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {u.t("foodAdviser:sessions_delete_warning")}
          </Typography>
          <Box display="flex" gap={1} justifyContent="flex-end">
            <Button
              onClick={() => setDeleteTarget(null)}
              disabled={!!deletingSessionId}
            >
              {u.t("foodAdviser:sessions_keep")}
            </Button>
            <Button
              variant="contained"
              color="error"
              onClick={handleConfirmDelete}
              disabled={!!deletingSessionId}
            >
              {deletingSessionId && deletingSessionId === deleteTarget?.id
                ? u.t("foodAdviser:polling")
                : u.t("foodAdviser:sessions_delete_submit")}
            </Button>
          </Box>
        </Stack>
      </CustomDialog>
    </Box>
  );
};

const mapStateToProps = () => ({});

export default connect(mapStateToProps)(FoodAdviserSessionsPage);
