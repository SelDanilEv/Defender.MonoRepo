import { ChangeEvent, useEffect, useState } from "react";
import {
  Divider,
  Box,
  Card,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TablePagination,
  TableRow,
  TableContainer,
  Typography,
  useTheme,
  CardHeader,
  Stack,
} from "@mui/material";
import { connect } from "react-redux";
import chroma from "chroma-js";
import AddIcon from "@mui/icons-material/Add";
import CachedIcon from "@mui/icons-material/Cached";
import DeleteIcon from "@mui/icons-material/Delete";
import EditNoteIcon from "@mui/icons-material/EditNote";
import VisibilityOffIcon from "@mui/icons-material/VisibilityOff";
import VisibilityIcon from "@mui/icons-material/Visibility";

import { CurrentPagination } from "src/models/base/CurrentPagination";
import useUtils from "src/appUtils";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import CustomDialog from "src/components/Dialog";
import { PaginationRequest } from "src/models/base/PaginationRequest";
import { DialogMode, OpenDialog } from "src/models/shared/DialogMode";
import LockedIconButton from "src/components/LockedComponents/LockedIconButtons/LockedIconButton";
import { BudgetDiagramGroup } from "src/models/budgetTracker/BudgetDiagramGroups";

import GroupDialogBody from "./GroupDialogBody";
import { UpdateGroup } from "./GroupDialogBody/actions";
import DefaultTableConsts from "src/consts/DefaultTableConsts";
import TagChip from "src/components/TagChip";

interface GroupsTableProps {
  groups: BudgetDiagramGroup[];
  applyPagination: (page: number, limit: number) => void;
  pagination: CurrentPagination;
  refresh: () => void;
}

const DefaultGroup: BudgetDiagramGroup = {
  id: "",
  name: "",
  isActive: false,
  tags: [],
  mainColor: chroma.random().hex(),
  showTrendLine: false,
  trendLineColor: chroma.random().hex(),
} as BudgetDiagramGroup;

const GroupsTable = (props: GroupsTableProps) => {
  const u = useUtils();
  const theme = useTheme();

  const {
    groups: groups,
    applyPagination: applyPagination,
    pagination: pagination,
    refresh: refresh,
  } = props;

  const [tablePagination, setTablePagination] = useState<PaginationRequest>({
    page: DefaultTableConsts.DefaultPage,
    pageSize: DefaultTableConsts.DefaultPageSize,
  });

  const [groupToUpdate, setModelToUpdate] = useState<BudgetDiagramGroup>();
  const [dialogMode, setDialogMode] = useState<DialogMode>(DialogMode.Hide);

  useEffect(() => {
    applyPagination(tablePagination.page, tablePagination.pageSize);
  }, [tablePagination]);

  const handlePageChange = (event: any, newPage: number): void => {
    setTablePagination({ ...tablePagination, page: newPage });
  };

  const handleLimitChange = (event: ChangeEvent<HTMLInputElement>): void => {
    setTablePagination({
      page: DefaultTableConsts.DefaultPage,
      pageSize: parseInt(event.target.value),
    });
  };

  const generateTags = (tags: string[]): JSX.Element => {
    return (
      <Box
        sx={{
          display: "flex",
          flexWrap: "wrap",
          justifyContent: "center",
          gap: 0.5,
        }}
      >
        {tags.map((tag, index) => (
          <TagChip
            sx={{
              height: 28,
              "& .MuiChip-label": {
                px: 1,
                fontSize: "0.82rem",
              },
            }}
            key={index}
            label={tag}
            size="small"
          />
        ))}
      </Box>
    );
  };

  const renderRowInfo = (model: BudgetDiagramGroup): JSX.Element => {
    {
      return (
        <TableRow hover key={model.id}>
          <TableCell align="center">
            <Typography
              variant="body2"
              fontWeight="bold"
              color="text.primary"
              noWrap
            >
              {model.name}
            </Typography>
          </TableCell>
          {u.isMobile ? null : (
            <TableCell align="center">
              {generateTags(model.tags)}
            </TableCell>
          )}
          <TableCell align="center">
            <Box
              sx={{
                width: 18,
                height: 18,
                backgroundColor: model.mainColor,
                borderRadius: "4px",
                display: "inline-block",
              }}
            />
          </TableCell>
          <TableCell align="center">
            {model.showTrendLine ? (
              <Box
                sx={{
                  width: 18,
                  height: 18,
                  backgroundColor: model.trendLineColor,
                  borderRadius: "4px",
                  display: "inline-block",
                }}
              />
            ) : (
              <LockedIconButton
                disabled
                color="inherit"
                size="small"
                sx={{ p: 0.4 }}
              >
                <VisibilityOffIcon fontSize="small" />
              </LockedIconButton>
            )}
          </TableCell>
          <TableCell align="center">
            <Stack direction="row" spacing={0.25} justifyContent="center">
              {model.isActive ? (
                <LockedIconButton
                  sx={{
                    p: 0.45,
                    "&:hover": { background: theme.colors.success.light },
                    color: theme.palette.success.light,
                  }}
                  onClick={(event) => {
                    event.stopPropagation();

                    UpdateGroup(
                      {
                        id: model.id,
                        isActive: !model.isActive,
                      } as BudgetDiagramGroup,
                      u,
                      refresh
                    );
                  }}
                  color="inherit"
                  size="small"
                >
                  <VisibilityIcon fontSize="small" />
                </LockedIconButton>
              ) : (
                <LockedIconButton
                  sx={{
                    p: 0.45,
                    "&:hover": { background: theme.colors.error.light },
                    color: theme.palette.error.light,
                  }}
                  onClick={(event) => {
                    event.stopPropagation();

                    UpdateGroup(
                      {
                        id: model.id,
                        isActive: !model.isActive,
                      } as BudgetDiagramGroup,
                      u,
                      refresh
                    );
                  }}
                  color="inherit"
                  size="small"
                >
                  <VisibilityOffIcon fontSize="small" />
                </LockedIconButton>
              )}

              <LockedIconButton
                sx={{
                  p: 0.45,
                  "&:hover": { background: theme.colors.warning.lighter },
                  color: theme.palette.warning.dark,
                }}
                onClick={(event) => {
                  event.stopPropagation();
                  setModelToUpdate(model);
                  setDialogMode(DialogMode.Update);
                }}
                color="inherit"
                size="small"
              >
                <EditNoteIcon fontSize="small" />
              </LockedIconButton>
              <LockedIconButton
                sx={{
                  p: 0.45,
                  "&:hover": { background: theme.colors.error.lighter },
                  color: theme.palette.error.dark,
                }}
                onClick={(event) => {
                  event.stopPropagation();
                  setModelToUpdate(model);
                  setDialogMode(DialogMode.Delete);
                }}
                color="inherit"
                size="small"
              >
                <DeleteIcon fontSize="small" />
              </LockedIconButton>
            </Stack>
          </TableCell>
        </TableRow>
      );
    }
  };

  return (
    <Card>
      <CardHeader
        action={
          <>
            <LockedButton
              sx={{
                mr: 1,
                minWidth: 44,
                width: 44,
                height: 44,
                p: 0,
              }}
              variant="outlined"
              color="success"
              onClick={() => {
                setModelToUpdate(DefaultGroup);
                setDialogMode(DialogMode.Create);
              }}
            >
              <AddIcon />
            </LockedButton>
            <LockedButton
              sx={{
                mr: 1,
                minWidth: 44,
                width: 44,
                height: 44,
                p: 0,
              }}
              variant="outlined"
              onClick={refresh}
            >
              <CachedIcon />
            </LockedButton>
          </>
        }
        title={
          <Typography fontSize={"1.7em"} fontWeight="bold">
            {u.t("budgetTracker:groups_table_title")}
          </Typography>
        }
        titleTypographyProps={{
          style: { fontSize: u.isMobile ? "1.3em" : "1.65em" },
        }}
        sx={{ py: 1.25, px: 2 }}
      />
      <Divider />
      <TableContainer
        sx={{
          "& .MuiTableCell-root": {
            px: u.isMobile ? 1 : 1.5,
            py: 1.25,
            fontSize: "0.88rem",
            lineHeight: 1.3,
          },
          "& .MuiTableHead-root .MuiTableCell-root": {
            py: 1.5,
            fontSize: "0.82rem",
            fontWeight: 700,
            letterSpacing: "0.03em",
          },
        }}
      >
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell align="center">
                {u.t("budgetTracker:groups_table_name_column")}
              </TableCell>
              {u.isMobile ? null : (
                <TableCell align="center">
                  {u.t("budgetTracker:groups_table_tags_column")}
                </TableCell>
              )}
              <TableCell align="center">
                {u.t("budgetTracker:groups_table_color_column")}
              </TableCell>
              <TableCell align="center">
                {u.t("budgetTracker:groups_table_trend_line_column")}
              </TableCell>
              <TableCell align="center">
                {u.t("table_actions_column")}
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>{groups.map(renderRowInfo)}</TableBody>
        </Table>
      </TableContainer>
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          justifyContent: "right",
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
            fontSize: u.isMobile ? "0.68rem" : "0.8rem",
            lineHeight: 1.2,
          }}
        >
          {u.t("table_rows_per_page_label")}
        </Typography>
        <TablePagination
          component="div"
          count={pagination.totalItemsCount}
          onPageChange={handlePageChange}
          onRowsPerPageChange={handleLimitChange}
          page={tablePagination.page}
          rowsPerPage={tablePagination.pageSize}
          rowsPerPageOptions={[10, 25, 50, 100]}
          labelRowsPerPage=""
          sx={{
            "& .MuiTablePagination-toolbar": {
              minHeight: 32,
              pl: 0,
              pr: 0,
              py: 0,
            },
            "& .MuiTablePagination-selectLabel, & .MuiTablePagination-displayedRows": {
              fontSize: "0.78rem",
              my: 0,
            },
            "& .MuiInputBase-root": {
              fontSize: "0.78rem",
              mr: 0.5,
            },
            "& .MuiTablePagination-select": {
              py: 0.25,
              minHeight: 0,
            },
            "& .MuiTablePagination-actions": {
              ml: 0.5,
            },
            "& .MuiTablePagination-actions .MuiIconButton-root": {
              p: 0.4,
            },
          }}
        />
      </Box>
      <CustomDialog
        title={u.t("budgetTracker:groups_dialog_title")}
        open={OpenDialog(dialogMode)}
        onClose={() => {
          setDialogMode(DialogMode.Hide);
          setModelToUpdate(DefaultGroup);
        }}
        children={
          <GroupDialogBody
            closeDialog={() => {
              setDialogMode(DialogMode.Hide);
              refresh();
            }}
            inputModel={groupToUpdate}
            dialogMode={dialogMode}
          />
        }
      />
    </Card>
  );
};

const mapStateToProps = (state: any) => {
  return {
    currentLanguage: state.session.language,
    walletNumber: state.wallet.walletNumber,
  };
};

export default connect(mapStateToProps)(GroupsTable);
