import { ChangeEvent, useEffect, useRef, useState } from "react";
import {
  Box,
  Card,
  CardHeader,
  Chip,
  Divider,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  Typography,
  useTheme,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import CachedIcon from "@mui/icons-material/Cached";
import DeleteIcon from "@mui/icons-material/Delete";
import EditNoteIcon from "@mui/icons-material/EditNote";

import useUtils from "src/appUtils";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import LockedIconButton from "src/components/LockedComponents/LockedIconButtons/LockedIconButton";
import CustomDialog from "src/components/Dialog";
import { compactIconButtonLayout } from "src/components/Buttons/buttonLayouts";
import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";
import DefaultTableConsts from "src/consts/DefaultTableConsts";
import type { CurrentPagination } from "src/models/base/CurrentPagination";
import type { PaginationRequest } from "src/models/base/PaginationRequest";
import type { RegularExpenseReview } from "src/models/budgetTracker/regularExpenses";
import { DialogMode, OpenDialog } from "src/models/shared/DialogMode";

import ReviewDialog from "../ReviewDialog";
import { calculateReviewTotalMonthlyMajor } from "../reviewData";

interface ReviewsTableProps {
  reviews: RegularExpenseReview[];
  pagination: CurrentPagination;
  applyPagination: (page: number, pageSize: number) => void;
  refresh: () => void;
}

const ReviewsTable = ({ reviews, pagination, applyPagination, refresh }: ReviewsTableProps) => {
  const u = useUtils();
  const theme = useTheme();
  const applyPaginationRef = useRef(applyPagination);
  applyPaginationRef.current = applyPagination;
  const [tablePagination, setTablePagination] = useState<PaginationRequest>({
    page: DefaultTableConsts.DefaultPage,
    pageSize: DefaultTableConsts.DefaultPageSize,
  });
  const [selectedReview, setSelectedReview] = useState<RegularExpenseReview>();
  const [dialogMode, setDialogMode] = useState<DialogMode>(DialogMode.Hide);

  useEffect(() => {
    applyPaginationRef.current(tablePagination.page, tablePagination.pageSize);
  }, [tablePagination]);

  const closeDialog = () => {
    setDialogMode(DialogMode.Hide);
    setSelectedReview(undefined);
    refresh();
  };

  return (
    <Card>
      <CardHeader
        action={
          <>
            <LockedButton
              aria-label={u.t("budgetTracker:regular_expenses_create_review")}
              sx={{ ...compactIconButtonLayout, mr: 1 }}
              variant="outlined"
              color="success"
              onClick={() => {
                setSelectedReview(undefined);
                setDialogMode(DialogMode.Create);
              }}
            ><AddIcon /></LockedButton>
            <LockedButton
              aria-label={u.t("Refresh")}
              sx={{ ...compactIconButtonLayout, mr: 1 }}
              variant="outlined"
              onClick={refresh}
            ><CachedIcon /></LockedButton>
          </>
        }
        title={<Typography sx={{ fontSize: "1.7em", fontWeight: "bold" }}>{u.t("budgetTracker:regular_expenses_create_review")}</Typography>}
      />
      <Divider />
      <TableContainer sx={{ overflowX: "auto", "& .MuiTableCell-root": { fontSize: u.isMobile ? "0.78em" : "0.9em" } }}>
        <Table aria-label={u.t("budgetTracker:regular_expenses_create_review")}>
          <TableHead><TableRow>
            <TableCell>{u.t("budgetTracker:regular_expenses_month_column")}</TableCell>
            <TableCell>{u.t("budgetTracker:regular_expenses_total_column")}</TableCell>
            {!u.isMobile && <TableCell>{u.t("budgetTracker:regular_expenses_count_column")}</TableCell>}
            <TableCell align="center">{u.t("table_actions_column")}</TableCell>
          </TableRow></TableHead>
          <TableBody>
            {reviews.length === 0 ? (
              <TableRow><TableCell colSpan={u.isMobile ? 3 : 4} align="center"><Typography color="text.secondary" sx={{ py: 2 }}>{u.t("budgetTracker:regular_expenses_no_reviews")}</Typography></TableCell></TableRow>
            ) : reviews.map((review) => {
              const currency = review.ratesModel?.baseCurrency;
              const total = calculateReviewTotalMonthlyMajor(review);
              return (
                <TableRow hover key={review.id}>
                  <TableCell><Typography sx={{ fontWeight: "bold" }}>{review.month.slice(0, 7)}</Typography></TableCell>
                  <TableCell><Chip label={`${total} ${CurrencySymbolsMap[currency] || currency}`} size="small" /></TableCell>
                  {!u.isMobile && <TableCell>{review.expenses.length}</TableCell>}
                  <TableCell align="center">
                    <LockedIconButton aria-label={`${u.t("Update")} ${review.month.slice(0, 7)}`} sx={{ "&:hover": { background: theme.colors.warning.lighter }, color: theme.palette.warning.dark }} onClick={() => { setSelectedReview(review); setDialogMode(DialogMode.Update); }} size="small"><EditNoteIcon fontSize="small" /></LockedIconButton>
                    <LockedIconButton aria-label={`${u.t("Delete")} ${review.month.slice(0, 7)}`} sx={{ "&:hover": { background: theme.colors.error.lighter }, color: theme.palette.error.dark }} onClick={() => { setSelectedReview(review); setDialogMode(DialogMode.Delete); }} size="small"><DeleteIcon fontSize="small" /></LockedIconButton>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "flex-end", flexWrap: "wrap", gap: 0.5, px: 1.5, pt: 0.5, pb: 0.75 }}>
        <Typography variant="body2" sx={{ fontSize: u.isMobile ? "0.68rem" : "0.8rem" }}>{u.t("table_rows_per_page_label")}</Typography>
        <TablePagination
          component="div"
          count={pagination.totalItemsCount}
          page={tablePagination.page}
          rowsPerPage={tablePagination.pageSize}
          onPageChange={(_event, page) => setTablePagination((current) => ({ ...current, page }))}
          onRowsPerPageChange={(event: ChangeEvent<HTMLInputElement>) => setTablePagination({ page: 0, pageSize: parseInt(event.target.value, 10) })}
          rowsPerPageOptions={[10, 25, 50, 100]}
          labelRowsPerPage=""
          slotProps={{ select: { inputProps: { "aria-label": u.t("table_rows_per_page_label") } } }}
        />
      </Box>
      <CustomDialog
        title={u.t("budgetTracker:regular_expenses_create_review")}
        open={OpenDialog(dialogMode)}
        onClose={() => { setDialogMode(DialogMode.Hide); setSelectedReview(undefined); }}
        disableBackdropClick={dialogMode !== DialogMode.Create}
        children={<ReviewDialog dialogMode={dialogMode} inputModel={selectedReview} closeDialog={closeDialog} />}
      />
    </Card>
  );
};

export default ReviewsTable;
