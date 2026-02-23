import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  Grid,
  LinearProgress,
  List,
  ListItem,
  ListItemText,
  Typography,
} from "@mui/material";
import { ChangeEvent, useCallback, useEffect, useRef, useState } from "react";
import { connect } from "react-redux";
import { useNavigate } from "react-router-dom";

import useUtils from "src/appUtils";
import { foodAdviserApi, MenuSessionDto } from "src/api/foodAdviser";

const POLL_INTERVAL_MS = 2000;
const POLL_MAX_ATTEMPTS = 90;
const STATUS_REVIEW = "Review";
const STATUS_FAILED = "Failed";

const FoodAdviserSessionNewPage = () => {
  const u = useUtils();
  const navigate = useNavigate();
  const [session, setSession] = useState<MenuSessionDto | null>(null);
  const [files, setFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);
  const [parsing, setParsing] = useState(false);
  const [pollTimedOut, setPollTimedOut] = useState(false);
  const pollAttemptsRef = useRef(0);

  const pollSession = useCallback(
    (sessionId: string) => {
      foodAdviserApi.getSession(sessionId, u).then((data) => {
        if (!data) return;

        setSession(data);

        if (data.status === STATUS_REVIEW) {
          setParsing(false);
          navigate(`/food-adviser/session/${sessionId}/review`, {
            replace: true,
          });
        } else if (data.status === STATUS_FAILED) {
          setParsing(false);
        }
      });
    },
    [u, navigate]
  );

  useEffect(() => {
    if (!session?.id || !parsing) return;

    pollAttemptsRef.current = 0;
    setPollTimedOut(false);

    const id = setInterval(() => {
      pollAttemptsRef.current += 1;

      if (pollAttemptsRef.current >= POLL_MAX_ATTEMPTS) {
        clearInterval(id);
        setParsing(false);
        setPollTimedOut(true);
        return;
      }

      pollSession(session.id);
    }, POLL_INTERVAL_MS);

    return () => clearInterval(id);
  }, [session?.id, parsing, pollSession]);

  const handleCreateSession = () => {
    setPollTimedOut(false);
    foodAdviserApi.createSession(u).then((data) => {
      setSession(data);
    });
  };

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const list = e.target.files ? Array.from(e.target.files) : [];

    if (list.length > 10) {
      u?.e?.("Maximum 10 images allowed.");
      return;
    }

    setPollTimedOut(false);
    setFiles(list);
  };

  const handleUploadAndParse = () => {
    if (!session?.id || files.length === 0) return;

    setPollTimedOut(false);
    setUploading(true);

    foodAdviserApi
      .uploadSessionImages(session.id, files, u)
      .then(() => {
        setUploading(false);
        return foodAdviserApi.requestParsing(session.id, u);
      })
      .then(() => {
        setParsing(true);
      })
      .catch(() => setUploading(false));
  };

  return (
    <Box>
      <Typography variant="h4" sx={{ mb: 2 }}>
        {u.t("foodAdviser:new_session")}
      </Typography>
      <Grid container spacing={2}>
        {!session ? (
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Button variant="contained" onClick={handleCreateSession}>
                  {u.t("foodAdviser:new_session")}
                </Button>
              </CardContent>
            </Card>
          </Grid>
        ) : (
          <>
            <Grid item xs={12} sm={7}>
              <Card>
                <CardContent>
                  <Typography variant="subtitle1" gutterBottom>
                    {u.t("foodAdviser:upload_images")}
                  </Typography>
                  {(uploading || parsing) && <LinearProgress sx={{ mb: 1.5 }} />}
                  <input
                    type="file"
                    accept="image/*"
                    multiple
                    onChange={handleFileChange}
                    style={{ marginBottom: 16, display: "block" }}
                  />
                  <Box display="flex" gap={1} flexWrap="wrap" alignItems="center">
                    <Button
                      variant="contained"
                      disabled={files.length === 0 || uploading}
                      onClick={handleUploadAndParse}
                    >
                      {uploading
                        ? u.t("foodAdviser:polling")
                        : u.t("foodAdviser:start_parsing")}
                    </Button>
                    {parsing && (
                      <Typography variant="body2" color="text.secondary">
                        {u.t("foodAdviser:status_parsing")}
                      </Typography>
                    )}
                  </Box>
                  {pollTimedOut && (
                    <Alert severity="warning" sx={{ mt: 1.5 }}>
                      Parsing is taking longer than expected. You can retry with clearer images.
                    </Alert>
                  )}
                  {session.status === STATUS_FAILED && (
                    <Alert severity="error" sx={{ mt: 1.5 }}>
                      Parsing failed. Please re-upload clearer images and try again.
                    </Alert>
                  )}
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={12} sm={5}>
              <Card sx={{ height: "100%" }}>
                <CardContent>
                  <Typography variant="subtitle2" color="text.secondary">
                    {u.t("foodAdviser:status_uploaded")}
                  </Typography>
                  <Typography variant="body1" sx={{ mt: 1 }}>
                    {session.status ?? "-"}
                  </Typography>
                  {files.length > 0 && (
                    <>
                      <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                        {files.length} file(s) selected
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
          </>
        )}
      </Grid>
    </Box>
  );
};

const mapStateToProps = () => ({});

export default connect(mapStateToProps)(FoodAdviserSessionNewPage);
