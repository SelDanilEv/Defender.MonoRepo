import { Grid, Typography } from "@mui/material";
import { useEffect, useRef, useState } from "react";
import useUtils from "src/appUtils";
import LockedTextField from "src/components/LockedComponents/LockedTextField/LockedTextField";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import CheckBoxButton from "src/components/Buttons/CheckBoxButton";
import { Currency } from "src/models/shared/Currency";
import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import RequestParamsBuilder from "src/api/APIWrapper/RequestParamsBuilder";
import apiUrls from "src/api/apiUrls";
import SearchLotteryTicketsRequest from "src/models/requests/games/lottery/SearchLotteryTicketsRequest";
import PurchaseLotteryTicketsRequest from "src/models/requests/games/lottery/PurchaseLotteryTicketsRequest";

interface TicketSelectionProps {
  drawNumber: number;
  currency: Currency;
  betAmount: number;
  selectedTickets: number[];
  selectTicket: (ticket: number) => void;
  unselectTicket: (ticket: number) => void;
}

const TicketSelection = (props: TicketSelectionProps) => {
  const u = useUtils();
  const reloadAvailableTicketsRef = useRef<(request: SearchLotteryTicketsRequest) => void>(() => undefined);

  const mobileAmount = 12;
  const desktopAmount = 25;

  const getAmountOfTicketsToDisplay = () =>
    u.isMobile ? mobileAmount : desktopAmount;

  var {
    drawNumber,
    selectedTickets,
    currency,
    betAmount,
    selectTicket,
    unselectTicket,
  } = props;

  const [totalBet, setTotalBet] = useState<number>(0);

  useEffect(() => {
    setTotalBet(Math.floor(betAmount * 100 * selectedTickets.length));
  }, [selectedTickets, betAmount]);

  const [ticketsToDisplay, setTicketsToDisplay] = useState<number[]>([]);

  const [searchRequest, setSearchRequest] =
    useState<SearchLotteryTicketsRequest>({
      drawNumber: drawNumber,
      amountOfTickets: 0,
      targetTicket: 0,
    } as SearchLotteryTicketsRequest);
  const initialSearchRequestRef = useRef(searchRequest);

  const setTargetTicket = (targetTicket: number) => {
    setSearchRequest({
      ...searchRequest,
      targetTicket: targetTicket,
    });
  };

  useEffect(() => {
    reloadAvailableTicketsRef.current(initialSearchRequestRef.current);
  }, []);

  const reloadAvailableTickets = (request: SearchLotteryTicketsRequest) => {
    request.amountOfTickets =
      getAmountOfTicketsToDisplay() - selectedTickets.length;

    const url =
      `${apiUrls.lottery.getAvailableTickets}` +
      `${RequestParamsBuilder.BuildQuery(request)}`;

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
        const newTickets: number[] = await response.json();

        let unselectedTickets = [
          ...newTickets.filter((t) => !selectedTickets.includes(t)),
        ];

        const addSearchedValue =
          unselectedTickets.includes(request.targetTicket) &&
          !selectedTickets.includes(request.targetTicket);

        if (addSearchedValue) {
          selectTicket(request.targetTicket);

          unselectedTickets = [
            request.targetTicket,
            ...unselectedTickets.filter((t) => t !== request.targetTicket),
          ];
        }

        setTicketsToDisplay([...selectedTickets, ...unselectedTickets]);
      },
      onFailure: async (response) => {},
      showError: true,
    });
  };
  reloadAvailableTicketsRef.current = reloadAvailableTickets;

  const checkWalletBalance = () => {
    APICallWrapper({
      url: `${apiUrls.banking.walletInfo}`,
      options: {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
        cache: "default",
      },
      utils: u,
      onSuccess: async (response) => {
        const walletInfo = await response.json();

        const account = walletInfo.currencyAccounts.find(
          (acc) => acc.currency === currency
        );

        if (!account) {
          u.e("CurrencyAccountNotFound");
          return;
        }

        if (account.balance < totalBet) {
          u.e("NotEnoughFunds");
          return;
        }

        proceedToPurchase();
      },
      showError: true,
    });
  };

  const proceedToPurchase = () => {
    APICallWrapper({
      url: `${apiUrls.lottery.purchaseTickets}`,
      options: {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          drawNumber: drawNumber,
          amount: betAmount * 100,
          currency: currency,
          ticketNumbers: selectedTickets,
        } as PurchaseLotteryTicketsRequest),
        cache: "default",
      },
      utils: u,
      onSuccess: async (response) => {
        const result = (await response.json()) as number[];

        u.react.navigate("/games/lottery");
      },
      showSuccess: true,
      showError: true,
    });
  };

  const renderTickets = () => {
    const tickets = ticketsToDisplay.slice(0, getAmountOfTicketsToDisplay());

    return tickets.map((ticket, index) => {
      const isSelected = selectedTickets.includes(ticket);

      return (
        <Grid
          key={ticket}
          container
          sx={{
            justifyContent: "center"
          }}
          size={{
            xs: 6,
            sm: 2.4
          }}>
          <CheckBoxButton
            isChecked={isSelected}
            fullWidth
            text={ticket.toString()}
            onCheck={() => {
              selectTicket(ticket);
            }}
            onUncheck={() => {
              unselectTicket(ticket);
            }}
          />
        </Grid>
      );
    });
  };

  const isFull = selectedTickets.length >= getAmountOfTicketsToDisplay();

  return (
    <Grid container spacing={2}>
      <Grid
        container
        spacing={1}
        size={{
          xs: 12,
          sm: 12
        }}>
        <Grid
          container
          sx={{
            alignItems: "center"
          }}
          size={{
            xs: 6,
            sm: 6
          }}>
          <Typography
            variant={u.isMobile ? "h4" : "h6"}
            sx={{
              paddingLeft: u.isMobile ? 0 : 2
            }}
          >
            {u.t("lottery:draw_available_tickets_title")}
          </Typography>
        </Grid>
        <Grid
          container
          sx={{
            justifyContent: "center"
          }}
          size={{
            xs: 6,
            sm: 2
          }}>
          <LockedButton
            disabled={isFull}
            fullWidth
            variant="outlined"
            onClick={() => {
              const request = { ...searchRequest, targetTicket: 0 };
              reloadAvailableTickets(request);
            }}
          >
            {u.t("lottery:draw_available_tickets_random_button")}
          </LockedButton>
        </Grid>
        <Grid
          size={{
            xs: 6,
            sm: 2
          }}>
          <LockedTextField
            disabled={isFull}
            label={u.t("lottery:draw_available_tickets_target_number_label")}
            value={searchRequest.targetTicket || ""}
            onChange={(e) => setTargetTicket(+e.target.value)}
            type="number"
          />
        </Grid>
        <Grid
          container
          sx={{
            justifyContent: "center"
          }}
          size={{
            xs: 6,
            sm: 2
          }}>
          <LockedButton
            disabled={
              isFull || selectedTickets.includes(searchRequest.targetTicket)
            }
            fullWidth
            variant="outlined"
            onClick={() => {
              reloadAvailableTickets(searchRequest);
            }}
          >
            {u.t("lottery:draw_available_tickets_search_button")}
          </LockedButton>
        </Grid>
      </Grid>
      <Grid
        container
        rowSpacing={2}
        columnSpacing={4}
        size={{
          xs: 12,
          sm: 12
        }}>
        {renderTickets()}
      </Grid>
      <Grid
        container
        spacing={1}
        size={{
          xs: 12,
          sm: 12
        }}>
        <Grid
          container
          sx={{
            alignItems: "center"
          }}
          size={{
            xs: 12,
            sm: 7
          }}>
          <Typography
            align={u.isMobile ? "center" : "left"}
            variant={u.isMobile ? "h4" : "h6"}
            sx={{
              paddingLeft: u.isMobile ? 0 : 5,
              width: "100%"
            }}>
            {u.t("lottery:draw_available_tickets_total_bet")}
            {totalBet / 100} {CurrencySymbolsMap[currency]}
          </Typography>
        </Grid>
        <Grid
          size={{
            xs: 6,
            sm: 2.5
          }}>
          <LockedButton
            fullWidth
            variant="outlined"
            onClick={() => u.react.navigate("/games/lottery")}
          >
            {u.t("lottery:draw_available_tickets_back_button")}
          </LockedButton>
        </Grid>
        <Grid
          size={{
            xs: 6,
            sm: 2.5
          }}>
          <LockedButton
            fullWidth
            variant="outlined"
            disabled={!selectedTickets.length || !betAmount}
            onClick={checkWalletBalance}
          >
            {u.t("lottery:draw_available_tickets_proceed_button")}
          </LockedButton>
        </Grid>
      </Grid>
    </Grid>
  );
};

export default TicketSelection;
