import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Divider,
  Grid,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import { connect } from "react-redux";
import { useNavigate } from "react-router-dom";

import useUtils from "src/appUtils";
import { foodAdviserApi, PreferencesDto } from "src/api/foodAdviser";

const parseLines = (text: string): string[] =>
  text
    .split(/[\n,]/)
    .map((s) => s.trim())
    .filter(Boolean);

const normalizeLines = (text: string): string[] =>
  Array.from(new Set(parseLines(text)));

const arraysEqual = (left: string[], right: string[]): boolean =>
  left.length === right.length && left.every((value, index) => value === right[index]);

const FoodAdviserHomePage = () => {
  const u = useUtils();
  const navigate = useNavigate();
  const [prefs, setPrefs] = useState<PreferencesDto | null>(null);
  const [likesText, setLikesText] = useState("");
  const [dislikesText, setDislikesText] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    foodAdviserApi.getPreferences(u).then((data) => {
      if (data) {
        setPrefs(data);
        setLikesText(data.likes.join("\n"));
        setDislikesText(data.dislikes.join("\n"));
      }
    });
  }, []);

  const likes = normalizeLines(likesText);
  const dislikes = normalizeLines(dislikesText);
  const hasChanges = prefs
    ? !arraysEqual(likes, prefs.likes || [])
      || !arraysEqual(dislikes, prefs.dislikes || [])
    : likes.length > 0 || dislikes.length > 0;

  const handleSave = () => {
    setSaving(true);
    foodAdviserApi
      .updatePreferences(likes, dislikes, u)
      .then((data) => {
        setPrefs(data);
      })
      .finally(() => setSaving(false));
  };

  return (
    <Box>
      <Typography variant="h4" sx={{ mb: 2 }}>
        {u.t("foodAdviser:page_title")}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Keep your likes and dislikes updated so recommendations can adapt to your taste.
      </Typography>
      <Grid container spacing={2}>
        <Grid item xs={12} sm={6}>
          <Card sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                {u.t("foodAdviser:preferences_likes_label")}
              </Typography>
              <TextField
                placeholder={u.t("foodAdviser:preferences_likes_placeholder")}
                multiline
                minRows={5}
                value={likesText}
                onChange={(e) => setLikesText(e.target.value)}
                fullWidth
                size="small"
              />
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6}>
          <Card sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                {u.t("foodAdviser:preferences_dislikes_label")}
              </Typography>
              <TextField
                placeholder={u.t(
                  "foodAdviser:preferences_dislikes_placeholder"
                )}
                multiline
                minRows={5}
                value={dislikesText}
                onChange={(e) => setDislikesText(e.target.value)}
                fullWidth
                size="small"
              />
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12}>
          <Card variant="outlined">
            <CardContent>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                <Box sx={{ minWidth: 180 }}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Likes
                  </Typography>
                  <Typography variant="h6">{likes.length}</Typography>
                </Box>
                <Box sx={{ minWidth: 180 }}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Dislikes
                  </Typography>
                  <Typography variant="h6">{dislikes.length}</Typography>
                </Box>
                <Divider orientation="vertical" flexItem sx={{ display: { xs: "none", md: "block" } }} />
                <Box sx={{ flex: 1 }}>
                  <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>
                    Quick preview
                  </Typography>
                  <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                    {likes.slice(0, 4).map((item) => (
                      <Chip key={`like-${item}`} size="small" color="success" label={item} />
                    ))}
                    {dislikes.slice(0, 4).map((item) => (
                      <Chip key={`dislike-${item}`} size="small" color="default" variant="outlined" label={item} />
                    ))}
                  </Stack>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12}>
          {hasChanges && (
            <Alert severity="info" sx={{ mb: 1.5 }}>
              You have unsaved preference changes.
            </Alert>
          )}
          <Box display="flex" gap={1} flexWrap="wrap">
            <Button
              variant="contained"
              onClick={handleSave}
              disabled={!hasChanges || saving}
            >
              {saving ? u.t("foodAdviser:polling") : u.t("foodAdviser:save")}
            </Button>
            <Button
              variant="outlined"
              onClick={() => navigate("/food-adviser/session/new")}
            >
              {u.t("foodAdviser:new_session")}
            </Button>
          </Box>
        </Grid>
      </Grid>
    </Box>
  );
};

const mapStateToProps = () => ({});

export default connect(mapStateToProps)(FoodAdviserHomePage);
