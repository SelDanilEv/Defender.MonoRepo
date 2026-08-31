import { Card } from "@mui/material";
import { useEffect, useRef, useState } from "react";

import useUtils from "src/appUtils";
import DefaultTableConsts from "src/consts/DefaultTableConsts";
import type { CurrentPagination } from "src/models/base/CurrentPagination";
import type { PaginationRequest } from "src/models/base/PaginationRequest";
import type { RegularExpenseReview } from "src/models/budgetTracker/regularExpenses";

import { getRegularExpenseReviews } from "../api";
import ReviewsTable from "./Table";

const RegularExpenseReviewsPage = () => {
  const u = useUtils();
  const [reviews, setReviews] = useState<RegularExpenseReview[]>([]);
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
    getRegularExpenseReviews(u, paginationRequest)
      .then((response) => {
        setReviews(response.items ?? []);
        setPagination(response);
      })
      .catch(() => undefined);
  };
  reloadItemsRef.current = load;

  useEffect(() => {
    reloadItemsRef.current();
  }, [paginationRequest]);

  const applyPagination = (page: number, pageSize: number) => {
    setPaginationRequest((current) => current.page === page && current.pageSize === pageSize ? current : { page, pageSize });
  };

  return (
    <Card>
      <ReviewsTable reviews={reviews} pagination={pagination} applyPagination={applyPagination} refresh={load} />
    </Card>
  );
};

export default RegularExpenseReviewsPage;
