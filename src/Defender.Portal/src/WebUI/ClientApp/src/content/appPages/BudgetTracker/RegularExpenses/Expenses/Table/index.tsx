import { ChangeEvent, useEffect, useRef, useState } from "react";
import {
  Box,
  Card,
  CardHeader,
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
import DefaultTableConsts from "src/consts/DefaultTableConsts";
import type { CurrentPagination } from "src/models/base/CurrentPagination";
import type { PaginationRequest } from "src/models/base/PaginationRequest";
import { BudgetTrackerAvailableCurrencies } from "src/models/shared/Currency";
import {
  RegularExpenseType,
  type RegularExpense,
} from "src/models/budgetTracker/regularExpenses";
import { DialogMode, OpenDialog } from "src/models/shared/DialogMode";

import ExpenseDialog from "../ExpenseDialog";
import { minorToMajor } from "../../money";

interface ExpensesTableProps {
  expenses: RegularExpense[];
  pagination: CurrentPagination;
  applyPagination: (page: number, pageSize: number) => void;
  refresh: () => void;
}

const emptyExpense: RegularExpense = {
  id: "",
  name: "",
  type: RegularExpenseType.Regular,
  currency: BudgetTrackerAvailableCurrencies[0],
  defaultAmount: 0,
  orderPriority: 0,
};

const typeLabelKey = (type: RegularExpenseType): string =>
  type === RegularExpenseType.Subscription
    ? "budgetTracker:regular_expenses_type_subscription"
    : type === RegularExpenseType.Annual
      ? "budgetTracker:regular_expenses_type_annual"
      : "budgetTracker:regular_expenses_type_regular";

const ExpensesTable = ({ expenses, pagination, applyPagination, refresh }: ExpensesTableProps) => {
  const u = useUtils();
  const theme = useTheme();
  const applyPaginationRef = useRef(applyPagination);
  applyPaginationRef.current = applyPagination;
  const [tablePagination, setTablePagination] = useState<PaginationRequest>({
    page: DefaultTableConsts.DefaultPage,
    pageSize: DefaultTableConsts.DefaultPageSize,
  });
  const [selectedExpense, setSelectedExpense] = useState<RegularExpense>(emptyExpense);
  const [dialogMode, setDialogMode] = useState<DialogMode>(DialogMode.Hide);

  useEffect(() => {
    applyPaginationRef.current(tablePagination.page, tablePagination.pageSize);
  }, [tablePagination]);

  const closeDialog = () => {
    setDialogMode(DialogMode.Hide);
    refresh();
  };

  return (
    <Card>
      <CardHeader
        action={
          <>
            <LockedButton
              aria-label={u.t("budgetTracker:regular_expenses_dialog_title")}
              sx={{ ...compactIconButtonLayout, mr: 1 }}
              variant="outlined"
              color="success"
              onClick={() => {
                setSelectedExpense(emptyExpense);
                setDialogMode(DialogMode.Create);
              }}
            >
              <AddIcon />
            </LockedButton>
            <LockedButton
              aria-label={u.t("Refresh")}
              sx={{ ...compactIconButtonLayout, mr: 1 }}
              variant="outlined"
              onClick={refresh}
            >
              <CachedIcon />
            </LockedButton>
          </>
        }
        title={
          <Typography sx={{ fontSize: "1.7em", fontWeight: "bold" }}>
            {u.t("budgetTracker:regular_expenses_table_title")}
          </Typography>
        }
      />
      <Divider />
      <TableContainer sx={{ overflowX: "auto" }}>
        <Table aria-label={u.t("budgetTracker:regular_expenses_table_title")}>
          <TableHead>
            <TableRow>
              <TableCell>{u.t("budgetTracker:regular_expenses_name_column")}</TableCell>
              {!u.isMobile && <TableCell>{u.t("budgetTracker:regular_expenses_type_column")}</TableCell>}
              <TableCell>{u.t("budgetTracker:regular_expenses_default_amount_column")}</TableCell>
              {!u.isMobile && <TableCell>{u.t("budgetTracker:regular_expenses_priority_column")}</TableCell>}
              <TableCell align="center">{u.t("table_actions_column")}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {expenses.length === 0 ? (
              <TableRow>
                <TableCell colSpan={u.isMobile ? 3 : 5} align="center">
                  <Typography color="text.secondary" sx={{ py: 2 }}>
                    {u.t("budgetTracker:regular_expenses_no_expenses")}
                  </Typography>
                </TableCell>
              </TableRow>
            ) : expenses.map((expense) => (
              <TableRow hover key={expense.id}>
                <TableCell>
                  <Typography sx={{ fontWeight: "bold" }} noWrap>{expense.name}</Typography>
                </TableCell>
                {!u.isMobile && (
                  <TableCell>{u.t(typeLabelKey(expense.type))}</TableCell>
                )}
                <TableCell>
                  <Typography component="span">{minorToMajor(expense.defaultAmount)} {expense.currency}</Typography>
                  {expense.type === RegularExpenseType.Annual && (
                    <Typography component="span" variant="caption" color="text.secondary" sx={{ display: "block" }}>
                      {u.t("budgetTracker:regular_expenses_monthly_contribution_column")}: {minorToMajor(expense.defaultAmount / 12)} {expense.currency}
                    </Typography>
                  )}
                </TableCell>
                {!u.isMobile && <TableCell>{expense.orderPriority}</TableCell>}
                <TableCell align="center">
                  <LockedIconButton
                    aria-label={`${u.t("Update")} ${expense.name}`}
                    sx={{ "&:hover": { background: theme.colors.warning.lighter }, color: theme.palette.warning.dark }}
                    onClick={() => {
                      setSelectedExpense(expense);
                      setDialogMode(DialogMode.Update);
                    }}
                    size="small"
                  >
                    <EditNoteIcon fontSize="small" />
                  </LockedIconButton>
                  <LockedIconButton
                    aria-label={`${u.t("Delete")} ${expense.name}`}
                    sx={{ "&:hover": { background: theme.colors.error.lighter }, color: theme.palette.error.dark }}
                    onClick={() => {
                      setSelectedExpense(expense);
                      setDialogMode(DialogMode.Delete);
                    }}
                    size="small"
                  >
                    <DeleteIcon fontSize="small" />
                  </LockedIconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "flex-end", flexWrap: "wrap", gap: 0.5, px: 1.5, pt: 0.5, pb: 0.75 }}>
        <Typography variant="body2">
          {u.t("table_rows_per_page_label")}
        </Typography>
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
        title={u.t("budgetTracker:regular_expenses_dialog_title")}
        open={OpenDialog(dialogMode)}
        onClose={() => setDialogMode(DialogMode.Hide)}
        children={<ExpenseDialog dialogMode={dialogMode} inputModel={selectedExpense} closeDialog={closeDialog} />}
      />
    </Card>
  );
};

export default ExpensesTable;
