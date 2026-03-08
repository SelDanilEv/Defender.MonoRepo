import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  IconButton,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import { useEffect, useRef, useState } from "react";
import type { Dispatch, KeyboardEvent, SetStateAction } from "react";
import { connect } from "react-redux";
import { useNavigate } from "react-router-dom";

import useUtils from "src/appUtils";
import { foodAdviserApi, PreferencesDto } from "src/api/foodAdviser";
import TagChip from "src/components/TagChip";

const normalizeItems = (items: string[]): string[] =>
  Array.from(
    new Set(
      items
        .map((item) => item.trim())
        .filter(Boolean)
    )
  );

const arraysEqual = (left: string[], right: string[]): boolean =>
  left.length === right.length && left.every((value, index) => value === right[index]);

const FoodAdviserHomePage = () => {
  const u = useUtils();
  const utilsRef = useRef(u);
  utilsRef.current = u;
  const navigate = useNavigate();
  const [prefs, setPrefs] = useState<PreferencesDto | null>(null);
  const [likes, setLikes] = useState<string[]>([]);
  const [dislikes, setDislikes] = useState<string[]>([]);
  const [likeDraft, setLikeDraft] = useState("");
  const [dislikeDraft, setDislikeDraft] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const loadPreferences = async () => {
      const data = await foodAdviserApi.getPreferences(utilsRef.current);

      if (data) {
        setPrefs(data);
        setLikes(normalizeItems(data.likes || []));
        setDislikes(normalizeItems(data.dislikes || []));
      }
    };

    void loadPreferences();
  }, []);

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

  const addItem = (
    draft: string,
    setter: Dispatch<SetStateAction<string[]>>,
    clearDraft: Dispatch<SetStateAction<string>>
  ) => {
    const value = draft.trim();
    if (!value) return;

    setter((prev) => normalizeItems([...prev, value]));
    clearDraft("");
  };

  const removeItem = (
    value: string,
    setter: Dispatch<SetStateAction<string[]>>
  ) => {
    setter((prev) => prev.filter((item) => item !== value));
  };

  const handleDraftKeyDown = (
    event: KeyboardEvent,
    draft: string,
    setter: Dispatch<SetStateAction<string[]>>,
    clearDraft: Dispatch<SetStateAction<string>>
  ) => {
    if (!["Enter", ",", "Tab"].includes(event.key)) return;

    event.preventDefault();
    addItem(draft, setter, clearDraft);
  };

  return (
    <Box sx={{ pt: 1 }}>
      <Box sx={{ mb: 2.5, textAlign: "center" }}>
        <Typography variant="h4" sx={{ mb: 1 }}>
          {u.t("foodAdviser:page_title")}
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ maxWidth: 560, mx: "auto" }}
        >
          {u.t("foodAdviser:home_subtitle")}
        </Typography>
      </Box>
      <Grid container spacing={2}>
        <Grid item xs={12} sm={6}>
          <Card sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                {u.t("foodAdviser:preferences_likes_label")}
              </Typography>
              <Stack spacing={1.5}>
                <Box
                  sx={{
                    minHeight: 120,
                    p: 1.5,
                    border: (theme) => `1px solid ${theme.palette.divider}`,
                    borderRadius: 2,
                    backgroundColor: "background.default",
                  }}
                >
                  <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                    {likes.map((item) => (
                      <TagChip
                        key={`like-${item}`}
                        tone="positive"
                        label={item}
                        onDelete={() => removeItem(item, setLikes)}
                      />
                    ))}
                    {likes.length === 0 && (
                      <Typography variant="body2" color="text.secondary">
                        {u.t("foodAdviser:preferences_likes_placeholder")}
                      </Typography>
                    )}
                  </Stack>
                </Box>
                <Box display="flex" gap={1} alignItems="center">
                  <TextField
                    placeholder={u.t("foodAdviser:new_item")}
                    value={likeDraft}
                    onChange={(e) => setLikeDraft(e.target.value)}
                    onKeyDown={(e) => handleDraftKeyDown(e, likeDraft, setLikes, setLikeDraft)}
                    fullWidth
                    size="small"
                  />
                  <IconButton
                    color="success"
                    onClick={() => addItem(likeDraft, setLikes, setLikeDraft)}
                    aria-label={u.t("foodAdviser:add")}
                  >
                    <AddIcon />
                  </IconButton>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6}>
          <Card sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                {u.t("foodAdviser:preferences_dislikes_label")}
              </Typography>
              <Stack spacing={1.5}>
                <Box
                  sx={{
                    minHeight: 120,
                    p: 1.5,
                    border: (theme) => `1px solid ${theme.palette.divider}`,
                    borderRadius: 2,
                    backgroundColor: "background.default",
                  }}
                >
                  <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                    {dislikes.map((item) => (
                      <TagChip
                        key={`dislike-${item}`}
                        label={item}
                        onDelete={() => removeItem(item, setDislikes)}
                      />
                    ))}
                    {dislikes.length === 0 && (
                      <Typography variant="body2" color="text.secondary">
                        {u.t("foodAdviser:preferences_dislikes_placeholder")}
                      </Typography>
                    )}
                  </Stack>
                </Box>
                <Box display="flex" gap={1} alignItems="center">
                  <TextField
                    placeholder={u.t("foodAdviser:new_item")}
                    value={dislikeDraft}
                    onChange={(e) => setDislikeDraft(e.target.value)}
                    onKeyDown={(e) => handleDraftKeyDown(e, dislikeDraft, setDislikes, setDislikeDraft)}
                    fullWidth
                    size="small"
                  />
                  <IconButton
                    color="default"
                    onClick={() => addItem(dislikeDraft, setDislikes, setDislikeDraft)}
                    aria-label={u.t("foodAdviser:add")}
                  >
                    <AddIcon />
                  </IconButton>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12}>
          {hasChanges && (
            <Alert severity="info" sx={{ mb: 1.5 }}>
              {u.t("foodAdviser:preferences_unsaved_changes")}
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
            <Button
              variant="outlined"
              onClick={() => navigate("/food-adviser/sessions")}
            >
              {u.t("foodAdviser:sessions_manage")}
            </Button>
          </Box>
        </Grid>
      </Grid>
    </Box>
  );
};

const mapStateToProps = () => ({});

export default connect(mapStateToProps)(FoodAdviserHomePage);
