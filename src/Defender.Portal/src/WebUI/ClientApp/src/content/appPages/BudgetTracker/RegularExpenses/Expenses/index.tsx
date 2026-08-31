import { Card } from "@mui/material";
import { useEffect, useRef, useState } from "react";

import useUtils from "src/appUtils";
import DefaultTableConsts from "src/consts/DefaultTableConsts";
import type { CurrentPagination } from "src/models/base/CurrentPagination";
import type { PaginationRequest } from "src/models/base/PaginationRequest";
import type { RegularExpense } from "src/models/budgetTracker/regularExpenses";

import { getRegularExpenses } from "../api";
import ExpensesTable from "./Table";

const ExpensesPage = () => {
  const u = useUtils();
  const [expenses, setExpenses] = useState<RegularExpense[]>([]);
  const [paginationRequest, setPaginationRequest] = useState<PaginationRequest>({
    page: DefaultTableConsts.DefaultPage,
    pageSize: DefaultTableConsts.DefaultPageSize,
  });
  const [pagination, setPagination] = useState<CurrentPagination>({
    totalItemsCount: 0,
    currentPage: 0,
    pageSize: DefaultTableConsts.DefaultPageSize,
    totalPagesCount: 1,
  });

  const reloadItemsRef = useRef<() => void>(() => undefined);

  const load = () => {
    getRegularExpenses(u, paginationRequest)
      .then((response) => {
        setExpenses(response.items ?? []);
        setPagination(response);
      })
      .catch(() => undefined);
  };
  reloadItemsRef.current = load;

  useEffect(() => {
    reloadItemsRef.current();
  }, [paginationRequest]);

  const applyPagination = (page: number, pageSize: number) => {
    setPaginationRequest((current) =>
      current.page === page && current.pageSize === pageSize
        ? current
        : { page, pageSize },
    );
  };

  return (
    <Card>
      <ExpensesTable
        expenses={expenses}
        pagination={pagination}
        applyPagination={applyPagination}
        refresh={load}
      />
    </Card>
  );
};

export default ExpensesPage;
