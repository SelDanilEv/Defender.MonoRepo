import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Typography } from "@mui/material";
import { useTranslation } from "react-i18next";

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  busy?: boolean;
  destructive?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}

export default function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel,
  busy = false,
  destructive = true,
  onCancel,
  onConfirm,
}: ConfirmDialogProps) {
  const { t } = useTranslation("myGarage");

  return (
    <Dialog open={open} onClose={busy ? undefined : onCancel}>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent><Typography>{message}</Typography></DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={busy}>{t("actions.cancel")}</Button>
        <Button color={destructive ? "error" : "primary"} variant="contained" onClick={onConfirm} disabled={busy}>{confirmLabel}</Button>
      </DialogActions>
    </Dialog>
  );
}
