import {
  Alert,
  alpha,
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  Grid,
  IconButton,
  LinearProgress,
  List,
  ListItem,
  ListItemText,
  Stack,
  Typography,
} from "@mui/material";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import UploadRoundedIcon from "@mui/icons-material/UploadRounded";
import ImageOutlinedIcon from "@mui/icons-material/ImageOutlined";
import { ChangeEvent, useCallback, useEffect, useRef, useState } from "react";
import type { DragEvent } from "react";
import { connect } from "react-redux";
import { useNavigate } from "react-router-dom";

import useUtils from "src/appUtils";
import { foodAdviserApi, MenuSessionDto } from "src/api/foodAdviser";

const MAX_IMAGES = 10;
const AUTO_POLL_INTERVAL_MS = 15_000;
const AUTO_POLL_WINDOW_MS = 3 * 60 * 1000;
const STATUS_REVIEW = "Review";
const STATUS_FAILED = "Failed";

const FoodAdviserSessionNewPage = () => {
  const u = useUtils();
  const navigate = useNavigate();
  const [session, setSession] = useState<MenuSessionDto | null>(null);
  const [files, setFiles] = useState<File[]>([]);
  const [creatingSession, setCreatingSession] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [parsing, setParsing] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [pollTimedOut, setPollTimedOut] = useState(false);
  const pollingStartedAtRef = useRef<number | null>(null);
  const pollingInFlightRef = useRef(false);
  const dragDepthRef = useRef(0);
  const [isDragActive, setIsDragActive] = useState(false);
  const isBusy = creatingSession || uploading || parsing;

  const getStatusLabel = (status: string | null | undefined) => {
    if (!status) {
      return u.t("foodAdviser:session_pending_creation");
    }

    const statusKey = `foodAdviser:status_${status.toLowerCase()}`;
    const localizedStatus = u.t(statusKey);

    return localizedStatus === statusKey ? status : localizedStatus;
  };

  const pollSession = useCallback(
    (sessionId: string) => {
      return foodAdviserApi.getSession(sessionId, u).then((data) => {
        if (!data) return;

        setSession(data);

        if (data.status === STATUS_REVIEW) {
          pollingStartedAtRef.current = null;
          setParsing(false);
          navigate(`/food-adviser/session/${sessionId}/review`, {
            replace: true,
          });
        } else if (data.status === STATUS_FAILED) {
          pollingStartedAtRef.current = null;
          setParsing(false);
        }
      });
    },
    [u, navigate]
  );

  useEffect(() => {
    if (!session?.id || !parsing) return;

    setPollTimedOut(false);
    pollingStartedAtRef.current = Date.now();
    pollingInFlightRef.current = false;

    const id = window.setInterval(() => {
      const startedAt = pollingStartedAtRef.current;

      if (!startedAt || Date.now() - startedAt >= AUTO_POLL_WINDOW_MS) {
        window.clearInterval(id);
        pollingStartedAtRef.current = null;
        setParsing(false);
        setPollTimedOut(true);
        return;
      }

      if (pollingInFlightRef.current) {
        return;
      }

      pollingInFlightRef.current = true;
      void pollSession(session.id).finally(() => {
        pollingInFlightRef.current = false;

        const nextStartedAt = pollingStartedAtRef.current;
        if (!nextStartedAt) {
          return;
        }

        if (Date.now() - nextStartedAt >= AUTO_POLL_WINDOW_MS) {
          pollingStartedAtRef.current = null;
          setParsing(false);
          setPollTimedOut(true);
        }
      });
    }, AUTO_POLL_INTERVAL_MS);

    return () => {
      window.clearInterval(id);
      pollingStartedAtRef.current = null;
      pollingInFlightRef.current = false;
    };
  }, [session?.id, parsing, pollSession]);

  const applySelectedFiles = useCallback((inputFiles: File[]) => {
    const imageFiles = inputFiles.filter((file) => file.type.startsWith("image/"));

    if (imageFiles.length !== inputFiles.length) {
      u?.e?.(u.t("foodAdviser:session_only_images_error"));
    }

    if (imageFiles.length === 0) {
      return;
    }

    if (imageFiles.length > MAX_IMAGES) {
      u?.e?.(u.t("foodAdviser:session_max_images_error"));
      setFiles(imageFiles.slice(0, MAX_IMAGES));
    } else {
      setFiles(imageFiles);
    }

    setPollTimedOut(false);
  }, [u]);

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const list = e.target.files ? Array.from(e.target.files) : [];
    applySelectedFiles(list);
    e.target.value = "";
  };

  const handleDragEnter = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    event.stopPropagation();

    if (isBusy) {
      return;
    }

    dragDepthRef.current += 1;
    setIsDragActive(true);
  };

  const handleDragOver = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    event.stopPropagation();
    event.dataTransfer.dropEffect = "copy";
  };

  const handleDragLeave = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    event.stopPropagation();

    if (isBusy) {
      return;
    }

    dragDepthRef.current = Math.max(0, dragDepthRef.current - 1);
    if (dragDepthRef.current === 0) {
      setIsDragActive(false);
    }
  };

  const handleDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    event.stopPropagation();

    dragDepthRef.current = 0;
    setIsDragActive(false);

    if (isBusy) {
      return;
    }

    const droppedFiles = Array.from(event.dataTransfer.files ?? []);
    applySelectedFiles(droppedFiles);
  };

  const handleRemoveFile = (indexToRemove: number) => {
    setFiles((currentFiles) => currentFiles.filter((_, index) => index !== indexToRemove));
  };

  const handleUploadAndParse = async () => {
    if (files.length === 0 || isBusy) return;

    setPollTimedOut(false);
    setUploading(true);

    try {
      let activeSession = session;

      if (!activeSession?.id) {
        setCreatingSession(true);
        activeSession = await foodAdviserApi.createSession(u);
        setSession(activeSession);
      }

      await foodAdviserApi.uploadSessionImages(activeSession.id, files, u);
      await foodAdviserApi.requestParsing(activeSession.id, u);
      setParsing(true);
    } catch {
      setParsing(false);
    } finally {
      setCreatingSession(false);
      setUploading(false);
    }
  };

  const handleRefresh = async () => {
    if (!session?.id || creatingSession || uploading || refreshing) {
      return;
    }

    setPollTimedOut(false);
    setRefreshing(true);

    try {
      await pollSession(session.id);
    } finally {
      setRefreshing(false);
    }
  };

  return (
    <Box sx={{ pt: 1 }}>
      <Box sx={{ mb: 2.5, textAlign: "center" }}>
        <Typography variant="h4" sx={{ mb: 1 }}>
          {u.t("foodAdviser:new_session")}
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ maxWidth: 560, mx: "auto" }}
        >
          {u.t("foodAdviser:session_new_hint")}
        </Typography>
      </Box>
      <Grid container spacing={2}>
        <Grid item xs={12} sm={7}>
          <Card>
            <CardContent>
              <Stack spacing={1.5}>
                <Typography variant="subtitle1">
                  {u.t("foodAdviser:upload_images")}
                </Typography>
                {isBusy && <LinearProgress />}
                <Box
                  sx={(theme) => ({
                    border: `1px dashed ${alpha(theme.palette.primary.main, 0.45)}`,
                    borderRadius: 2,
                    px: { xs: 1.5, sm: 2 },
                    py: { xs: 1.75, sm: 2.25 },
                    backgroundColor: isDragActive
                      ? alpha(theme.palette.primary.main, 0.12)
                      : alpha(theme.palette.primary.main, 0.05),
                    transition: "border-color 0.2s ease, background-color 0.2s ease",
                    borderColor: isDragActive
                      ? theme.palette.primary.main
                      : alpha(theme.palette.primary.main, 0.45),
                  })}
                  onDragEnter={handleDragEnter}
                  onDragOver={handleDragOver}
                  onDragLeave={handleDragLeave}
                  onDrop={handleDrop}
                >
                  <Stack spacing={1.5} alignItems={{ xs: "stretch", sm: "flex-start" }}>
                    <Box
                      sx={(theme) => ({
                        width: 38,
                        height: 38,
                        borderRadius: 1.5,
                        display: "inline-flex",
                        alignItems: "center",
                        justifyContent: "center",
                        backgroundColor: alpha(theme.palette.primary.main, 0.14),
                        color: theme.palette.primary.main,
                      })}
                    >
                      <UploadRoundedIcon fontSize="small" />
                    </Box>
                    <Box>
                      <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
                        {isDragActive
                          ? u.t("foodAdviser:session_drop_title")
                          : u.t("foodAdviser:session_picker_title")}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {isDragActive
                          ? u.t("foodAdviser:session_drop_hint")
                          : u.t("foodAdviser:session_picker_hint")}
                      </Typography>
                    </Box>
                    <Stack
                      direction={{ xs: "column", sm: "row" }}
                      spacing={1}
                      alignItems={{ xs: "stretch", sm: "center" }}
                    >
                      <Button
                        component="label"
                        variant="outlined"
                        size="small"
                        startIcon={<UploadRoundedIcon fontSize="small" />}
                        disabled={isBusy}
                      >
                        {files.length > 0
                          ? u.t("foodAdviser:session_replace_images")
                          : u.t("foodAdviser:session_select_images")}
                        <input
                          hidden
                          type="file"
                          accept="image/*"
                          multiple
                          onChange={handleFileChange}
                          disabled={isBusy}
                        />
                      </Button>
                      <Typography variant="body2" color="text.secondary">
                        {u.t("foodAdviser:session_selected_files")}: {files.length}
                      </Typography>
                    </Stack>
                  </Stack>
                </Box>
                {files.length > 0 && (
                  <Box
                    sx={(theme) => ({
                      border: `1px solid ${theme.palette.divider}`,
                      borderRadius: 2,
                      px: 1.5,
                      py: 1,
                    })}
                  >
                    <List dense disablePadding>
                      {files.slice(0, 5).map((file, index) => (
                        <ListItem
                          key={`${file.name}-${index}`}
                          disablePadding
                          sx={{ py: 0.5, gap: 1 }}
                          secondaryAction={(
                            <IconButton
                              edge="end"
                              size="small"
                              color="error"
                              aria-label={u.t("foodAdviser:session_remove_image")}
                              onClick={() => handleRemoveFile(index)}
                            >
                              <DeleteOutlineIcon fontSize="small" />
                            </IconButton>
                          )}
                        >
                          <ListItemText
                            primary={
                              <Box display="flex" alignItems="center" gap={1}>
                                <ImageOutlinedIcon fontSize="inherit" />
                                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                  {file.name}
                                </Typography>
                              </Box>
                            }
                            secondary={`${Math.max(1, Math.round(file.size / 1024))} KB`}
                            sx={{ pr: 5 }}
                          />
                        </ListItem>
                      ))}
                    </List>
                    {files.length > 5 && (
                      <Typography variant="caption" color="text.secondary">
                        {u.t("foodAdviser:session_more_files_hint")}: {files.length - 5}
                      </Typography>
                    )}
                  </Box>
                )}
              </Stack>
              <Box display="flex" gap={1} flexWrap="wrap" alignItems="center" sx={{ mt: 1.5 }}>
                <Button
                  variant="outlined"
                  onClick={() => navigate("/food-adviser")}
                  disabled={isBusy}
                >
                  {u.t("foodAdviser:back")}
                </Button>
                <Button
                  variant="contained"
                  disabled={files.length === 0 || isBusy}
                  onClick={handleUploadAndParse}
                >
                  {isBusy
                    ? u.t("foodAdviser:polling")
                    : u.t("foodAdviser:start_parsing")}
                </Button>
                <Button
                  variant="outlined"
                  onClick={handleRefresh}
                  disabled={!session?.id || creatingSession || uploading || refreshing}
                >
                  {refreshing
                    ? u.t("foodAdviser:polling")
                    : u.t("foodAdviser:recommendations_refresh")}
                </Button>
                {creatingSession && (
                  <Typography variant="body2" color="text.secondary">
                    {u.t("foodAdviser:session_creating")}
                  </Typography>
                )}
                {!creatingSession && parsing && (
                  <Typography variant="body2" color="text.secondary">
                    {u.t("foodAdviser:status_parsing")}
                  </Typography>
                )}
              </Box>
              {pollTimedOut && (
                <Alert severity="warning" sx={{ mt: 1.5 }}>
                  {u.t("foodAdviser:session_parsing_timeout_warning")}
                </Alert>
              )}
              {session?.status === STATUS_FAILED && (
                <Alert severity="error" sx={{ mt: 1.5 }}>
                  {u.t("foodAdviser:session_parsing_failed_warning")}
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={5}>
          <Card sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary">
                {u.t("foodAdviser:session_status")}
              </Typography>
              <Typography variant="body1" sx={{ mt: 1 }}>
                {creatingSession
                  ? u.t("foodAdviser:session_creating")
                  : getStatusLabel(session?.status)}
              </Typography>
              {!!session?.id && (
                <>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    {session.id}
                  </Typography>
                  <Divider sx={{ my: 1 }} />
                </>
              )}
              {files.length > 0 && (
                <>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    {u.t("foodAdviser:session_selected_files")}: {files.length}
                  </Typography>
                  <Divider sx={{ my: 1 }} />
                  <List dense disablePadding>
                    {files.slice(0, 5).map((file) => (
                      <ListItem key={file.name} disablePadding sx={{ py: 0.25 }}>
                        <ListItemText
                          primary={file.name}
                          secondary={`${Math.max(1, Math.round(file.size / 1024))} KB`}
                        />
                      </ListItem>
                    ))}
                  </List>
                </>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

const mapStateToProps = () => ({});

export default connect(mapStateToProps)(FoodAdviserSessionNewPage);
