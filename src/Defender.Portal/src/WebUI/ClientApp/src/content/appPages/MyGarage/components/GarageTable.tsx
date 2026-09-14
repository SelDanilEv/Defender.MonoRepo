import {
  Box,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";

interface GarageTableProps {
  ariaLabel: string;
  headers: ReactNode[];
  children: ReactNode;
  empty?: boolean;
  emptyMessage?: string;
  loading?: boolean;
}

export default function GarageTable({
  ariaLabel,
  headers,
  children,
  empty = false,
  emptyMessage,
  loading = false,
}: GarageTableProps) {
  const { t } = useTranslation("myGarage");
  const visibleHeaders = headers.filter((header) => header !== null && header !== undefined);

  return (
    <TableContainer sx={{ overflowX: "auto" }}>
      <Table aria-label={ariaLabel} size="small" sx={{ minWidth: { xs: 760, sm: "auto" } }}>
        <TableHead>
          <TableRow>
            {visibleHeaders.map((header, index) => <TableCell key={index}>{header}</TableCell>)}
          </TableRow>
        </TableHead>
        <TableBody>
          {loading ? (
            <TableRow>
              <TableCell colSpan={visibleHeaders.length}>
                <Box sx={{ display: "flex", justifyContent: "center", py: 3 }}>
                  <CircularProgress size={24} aria-label={t("loading")} />
                </Box>
              </TableCell>
            </TableRow>
          ) : empty ? (
            <TableRow>
              <TableCell colSpan={visibleHeaders.length}>
                <Typography color="text.secondary" sx={{ py: 3, textAlign: "center" }}>
                  {emptyMessage}
                </Typography>
              </TableCell>
            </TableRow>
          ) : children}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
