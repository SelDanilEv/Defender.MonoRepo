import { Card } from "@mui/material";
import HistoricalTicketsTable from "./Table";
import { useEffect, useRef, useState } from "react";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import apiUrls from "src/api/apiUrls";
import { connect } from "react-redux";
import useUtils from "src/appUtils";
import { CurrentPagination } from "src/models/base/CurrentPagination";
import RequestParamsBuilder from "src/api/APIWrapper/RequestParamsBuilder";
import LotteryTicket from "src/models/games/lottery/LotteryTicket";
import { PaginationRequest } from "src/models/base/PaginationRequest";
import TicketHistoryResponse from "src/models/responses/games/lottery/TransactionHistoryResponse";

interface HistoricalTicketsProps {
  SetLatestTickets: (tickets: LotteryTicket[]) => void;
}

const HistoricalTickets = (props: HistoricalTicketsProps) => {
  const u = useUtils();
  const reloadTicketHistoryRef = useRef<() => void>(() => undefined);

  const [tickets, setTickets] = useState<LotteryTicket[]>([]);

  const [paginationRequest, setPaginationRequest] = useState<PaginationRequest>(
    {
      page: 0,
      pageSize: 10,
    } as PaginationRequest
  );

  const applyPagination = (page: number, limit: number) => {
    if (paginationRequest.page === page && paginationRequest.pageSize === limit)
      return;
    setPaginationRequest({ ...paginationRequest, page, pageSize: limit });
  };

  const [pagination, setPagination] = useState<CurrentPagination>({
    totalItemsCount: 0,
    currentPage: 0,
    pageSize: 10,
    totalPagesCount: 1,
  } as CurrentPagination);

  useEffect(() => {
    reloadTicketHistoryRef.current();
  }, [paginationRequest]);

  const reloadTicketHistory = () => {
    const url =
      `${apiUrls.lottery.getMyTickets}` +
      `${RequestParamsBuilder.BuildQuery(paginationRequest)}`;

    APICallWrapper({
      url: url,
      options: {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      },
      utils: u,
      onSuccess: async (response) => {
        const ticketHistory: TicketHistoryResponse = await response.json();
        if (paginationRequest.page === 0)
          props.SetLatestTickets(ticketHistory.items);
        setTickets(ticketHistory.items);
        setPagination(ticketHistory);
      },
      onFailure: async (response) => {},
      showError: true,
    });
  };
  reloadTicketHistoryRef.current = reloadTicketHistory;

  return (
    <Card>
      <HistoricalTicketsTable
        tickets={tickets}
        applyPagination={applyPagination}
        pagination={pagination}
        refresh={reloadTicketHistory}
      />
    </Card>
  );
};

const mapStateToProps = (state: any) => {
  return {};
};

export default connect(mapStateToProps)(HistoricalTickets);
