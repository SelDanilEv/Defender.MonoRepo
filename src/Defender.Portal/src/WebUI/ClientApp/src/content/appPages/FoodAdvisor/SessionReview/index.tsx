import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  FormControlLabel,
  Grid,
  IconButton,
  TextField,
  Typography,
} from "@mui/material";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import { useEffect, useState } from "react";
import { connect } from "react-redux";
import { useNavigate, useParams } from "react-router-dom";

import useUtils from "src/appUtils";
import { foodAdvisorApi, MenuSessionDto } from "src/api/foodAdvisor";

const FoodAdvisorSessionReviewPage = () => {
  const u = useUtils();
  const navigate = useNavigate();
  const { sessionId } = useParams<{ sessionId: string }>();
  const [session, setSession] = useState<MenuSessionDto | null>(null);
  const [items, setItems] = useState<string[]>([]);
  const [trySomethingNew, setTrySomethingNew] = useState(false);
  const [newItemText, setNewItemText] = useState("");
  const [confirming, setConfirming] = useState(false);
  const cleanedItems = Array.from(
    new Set(items.map((item) => item.trim()).filter(Boolean))
  );

  useEffect(() => {
    if (!sessionId) return;
    foodAdvisorApi.getSession(sessionId, u).then((data) => {
      if (data) {
        setSession(data);
        setItems([...(data.parsedItems || [])]);
        setTrySomethingNew(data.trySomethingNew ?? false);
      }
    });
  }, [sessionId]);

  const handleRemove = (index: number) => {
    setItems((prev) => prev.filter((_, i) => i !== index));
  };

  const handleEdit = (index: number, value: string) => {
    setItems((prev) => {
      const next = [...prev];
      next[index] = value;
      return next;
    });
  };

  const handleAdd = () => {
    const t = newItemText.trim();
    if (!t) return;
    setItems((prev) => [...prev, t]);
    setNewItemText("");
  };

  const handleConfirm = async () => {
    if (!sessionId || confirming) return;
    setConfirming(true);
    try {
      await foodAdvisorApi.confirmMenu(sessionId, cleanedItems, trySomethingNew, u);
      setConfirming(false);
      navigate(`/food-advisor/session/${sessionId}/recommendations`);
      void foodAdvisorApi.requestRecommendations(sessionId, u).catch(() => {});
    } catch {
      setConfirming(false);
    }
  };

  if (!session) return null;

  return (
    <Box sx={{ pt: 1 }}>
      <Box sx={{ mb: 2.5, textAlign: "center" }}>
        <Typography variant="h4" sx={{ mb: 1 }}>
          {u.t("foodAdvisor:review_title")}
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ maxWidth: 560, mx: "auto" }}
        >
          {items.length === 0
            ? u.t("foodAdvisor:review_no_items_detected")
            : u.t("foodAdvisor:review_edit_hint")}
        </Typography>
      </Box>
      {cleanedItems.length !== items.length && (
        <Alert severity="info" sx={{ mb: 2 }}>
          {u.t("foodAdvisor:review_duplicate_entries_warning")}
        </Alert>
      )}
      <Grid container spacing={2}>
        {items.map((item, index) => (
          <Grid item xs={12} sm={6} md={4} key={index}>
            <Card variant="outlined" sx={{ height: "100%" }}>
              <CardContent sx={{ py: 1, "&:last-child": { pb: 1 } }}>
                <Box display="flex" alignItems="flex-start" gap={0.5}>
                  <TextField
                    size="small"
                    value={item}
                    onChange={(e) => handleEdit(index, e.target.value)}
                    fullWidth
                    variant="standard"
                  />
                  <IconButton
                    size="small"
                    color="error"
                    onClick={() => handleRemove(index)}
                    aria-label={u.t("foodAdvisor:review_remove_item")}
                  >
                    <DeleteOutlineIcon fontSize="small" />
                  </IconButton>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ))}
        <Grid item xs={12} sm={6} md={4}>
          <Card variant="outlined" sx={{ height: "100%", borderStyle: "dashed" }}>
            <CardContent sx={{ py: 1, "&:last-child": { pb: 1 } }}>
              <Box display="flex" gap={0.5} alignItems="center">
                <TextField
                  size="small"
                  placeholder={u.t("foodAdvisor:new_item")}
                  value={newItemText}
                  onChange={(e) => setNewItemText(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && handleAdd()}
                  variant="standard"
                  fullWidth
                />
                <Button size="small" variant="outlined" onClick={handleAdd}>
                  {u.t("foodAdvisor:add")}
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12}>
          <FormControlLabel
            control={
              <Checkbox
                checked={trySomethingNew}
                onChange={(e) => setTrySomethingNew(e.target.checked)}
              />
            }
            label={u.t("foodAdvisor:try_something_new")}
          />
        </Grid>
        <Grid item xs={12}>
          <Box display="flex" gap={1} flexWrap="wrap">
            <Button
              variant="contained"
              onClick={handleConfirm}
              disabled={cleanedItems.length === 0 || confirming}
            >
              {u.t("foodAdvisor:confirm_menu")}
            </Button>
            <Button
              variant="outlined"
              onClick={() => navigate("/food-advisor")}
            >
              {u.t("foodAdvisor:back")}
            </Button>
          </Box>
        </Grid>
      </Grid>
    </Box>
  );
};

const mapStateToProps = () => ({});

export default connect(mapStateToProps)(FoodAdvisorSessionReviewPage);
