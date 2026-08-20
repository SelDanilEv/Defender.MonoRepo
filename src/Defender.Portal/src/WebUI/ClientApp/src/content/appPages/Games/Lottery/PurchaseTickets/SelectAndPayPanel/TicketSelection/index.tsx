import AutoAwesomeIcon from "@mui/icons-material/AutoAwesome";
import CasinoIcon from "@mui/icons-material/Casino";
import ConfirmationNumberIcon from "@mui/icons-material/ConfirmationNumber";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import SearchIcon from "@mui/icons-material/Search";
import { Box, ButtonBase, Chip, Grid, LinearProgress, Typography } from "@mui/material";
import { useEffect, useRef, useState } from "react";
import useUtils from "src/appUtils";
import LockedTextField from "src/components/LockedComponents/LockedTextField/LockedTextField";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import { Currency } from "src/models/shared/Currency";
import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import RequestParamsBuilder from "src/api/APIWrapper/RequestParamsBuilder";
import apiUrls from "src/api/apiUrls";
import SearchLotteryTicketsRequest from "src/models/requests/games/lottery/SearchLotteryTicketsRequest";
import PurchaseLotteryTicketsRequest from "src/models/requests/games/lottery/PurchaseLotteryTicketsRequest";
import {
  getLotteryTicketSelectionProgress,
  getRandomAvailableLotteryTicket,
} from "../../../lotteryPresentation";

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
  const theme = u.react.theme;
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
      const isLocked = isFull && !isSelected;

      return (
        <Grid
          key={ticket}
          sx={{
            display: "flex",
          }}
          size={{
            xs: 6,
            sm: 2.4
          }}>
          <ButtonBase
            aria-label={ticket.toString()}
            aria-pressed={isSelected}
            disabled={isLocked}
            onClick={() => {
              if (isSelected) {
                unselectTicket(ticket);
              } else {
                selectTicket(ticket);
              }
            }}
            sx={{
              position: "relative",
              display: "block",
              width: "100%",
              minHeight: { xs: 68, sm: 82 },
              overflow: "hidden",
              borderRadius: theme.general.borderRadius,
              border: `1px solid ${isSelected ? theme.colors.primary.main : theme.colors.alpha.black[10]}`,
              color: isSelected
                ? theme.colors.alpha.trueWhite[100]
                : theme.palette.text.primary,
              background: isSelected
                ? theme.colors.gradients.purple3
                : theme.palette.mode === "dark"
                  ? theme.colors.alpha.trueWhite[5]
                  : theme.colors.alpha.black[5],
              boxShadow: isSelected ? theme.colors.shadows.primary : "none",
              transition: "transform .2s ease, box-shadow .2s ease, border-color .2s ease, background .2s ease",
              animation: isSelected ? "lottery-ticket-pop .36s ease-out" : "none",
              "@keyframes lottery-ticket-pop": {
                "0%": { transform: "scale(.92)", opacity: 0.7 },
                "100%": { transform: "scale(1)", opacity: 1 },
              },
              "&:hover": isLocked
                ? undefined
                : {
                    transform: "translateY(-3px)",
                    borderColor: theme.colors.primary.main,
                    boxShadow: isSelected
                      ? theme.colors.shadows.primary
                      : `0 8px 18px ${theme.colors.alpha.black[10]}`,
                  },
              "&:focus-visible": {
                outline: `3px solid ${theme.colors.info.main}`,
                outlineOffset: 2,
              },
              "&.Mui-disabled": {
                opacity: 0.42,
                cursor: "not-allowed",
              },
            }}
          >
            <Box
              sx={{
                display: "flex",
                minHeight: "inherit",
                flexDirection: "column",
                justifyContent: "space-between",
                alignItems: "stretch",
                p: 1,
                textAlign: "left",
              }}
            >
              <Typography
                variant="caption"
                sx={{
                  color: isSelected
                    ? theme.colors.alpha.trueWhite[70]
                    : theme.colors.alpha.black[70],
                  fontWeight: 800,
                  letterSpacing: "0.08em",
                  lineHeight: 1,
                }}
              >
                #{String(index + 1).padStart(2, "0")}
              </Typography>
              <Typography
                variant="h5"
                sx={{
                  color: "inherit",
                  fontWeight: 900,
                  lineHeight: 1,
                }}
              >
                {ticket}
              </Typography>
              <Box sx={{ display: "flex", justifyContent: "flex-end", minHeight: 16 }}>
                {isSelected ? (
                  <CheckCircleIcon sx={{ fontSize: 16, color: theme.colors.alpha.trueWhite[100] }} />
                ) : (
                  <AutoAwesomeIcon
                    sx={{
                      fontSize: 14,
                      color: theme.palette.mode === "dark"
                        ? theme.colors.alpha.trueWhite[30]
                        : theme.colors.alpha.black[30],
                    }}
                  />
                )}
              </Box>
            </Box>
          </ButtonBase>
        </Grid>
      );
    });
  };

  const isFull = selectedTickets.length >= getAmountOfTicketsToDisplay();
  const boardSize = getAmountOfTicketsToDisplay();
  const selectionProgress = getLotteryTicketSelectionProgress(
    selectedTickets.length,
    boardSize,
  );

  const pickRandomTicket = () => {
    if (isFull) {
      return;
    }

    const randomTicket = getRandomAvailableLotteryTicket(
      ticketsToDisplay,
      selectedTickets,
      Math.random(),
    );

    if (randomTicket === null) {
      reloadAvailableTickets({ ...searchRequest, targetTicket: 0 });
      return;
    }

    selectTicket(randomTicket);
  };

  return (
    <Grid container spacing={2}>
      <Grid size={{ xs: 12 }}>
        <Grid container spacing={1} sx={{ alignItems: "center" }}>
          <Grid size={{ xs: 8, sm: 9 }}>
            <Typography variant="overline" sx={{ color: theme.palette.primary.main, fontWeight: 800 }}>
              {u.t("lottery:draw_entry_kicker")}
            </Typography>
            <Typography variant={u.isMobile ? "h5" : "h4"} sx={{ fontWeight: 900, lineHeight: 1.05 }}>
              {u.t("lottery:draw_available_tickets_title")}
            </Typography>
          </Grid>
          <Grid size={{ xs: 4, sm: 3 }} container sx={{ justifyContent: "flex-end" }}>
            <Chip
              icon={<ConfirmationNumberIcon />}
              label={`${selectedTickets.length}/${getAmountOfTicketsToDisplay()}`}
              size="small"
              sx={{
                color: theme.palette.primary.main,
                backgroundColor: theme.colors.primary.lighter,
                "& .MuiChip-icon": { color: theme.palette.primary.main },
              }}
            />
          </Grid>
        </Grid>
        <Box
          sx={{
            mt: 1.5,
            p: 1.25,
            borderRadius: theme.general.borderRadius,
            border: `1px solid ${theme.colors.primary.light}`,
            background: theme.palette.mode === "dark"
              ? `linear-gradient(110deg, ${theme.colors.alpha.trueWhite[5]}, ${theme.colors.primary.lighter})`
              : `linear-gradient(110deg, ${theme.colors.alpha.black[5]}, ${theme.colors.primary.lighter})`,
          }}
        >
          <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 0.75 }}>
            <Typography variant="caption" sx={{ color: theme.palette.primary.main, fontWeight: 900, letterSpacing: "0.08em" }}>
              {u.t("lottery:draw_entry_ticket_progress")}
            </Typography>
            <Typography variant="caption" sx={{ color: "text.primary", fontWeight: 900 }}>
              {selectedTickets.length}/{boardSize} · {selectionProgress}%
            </Typography>
          </Box>
          <LinearProgress
            variant="determinate"
            value={selectionProgress}
            aria-label={u.t("lottery:draw_entry_ticket_progress")}
            sx={{
              height: 8,
              borderRadius: 4,
              backgroundColor: theme.colors.alpha.black[10],
              "& .MuiLinearProgress-bar": {
                borderRadius: 4,
                background: theme.colors.gradients.purple3,
              },
            }}
          />
        </Box>
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
            xs: 6,
            sm: 6
          }}>
            <Typography variant="body2" color="text.secondary" sx={{ paddingLeft: u.isMobile ? 0 : 2 }}>
              {u.t("lottery:draw_entry_ticket_hint")}
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
            startIcon={<CasinoIcon />}
            fullWidth
            variant="outlined"
            onClick={pickRandomTicket}
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
            startIcon={<SearchIcon />}
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
        role="group"
        aria-label={u.t("lottery:draw_available_tickets_title")}
        sx={{
          position: "relative",
          overflow: "hidden",
          p: { xs: 1, sm: 1.5 },
          borderRadius: theme.general.borderRadiusLg,
          border: `1px solid ${theme.colors.primary.light}`,
          background: theme.palette.mode === "dark"
            ? `radial-gradient(circle at 15% 0%, ${theme.colors.primary.lighter}, transparent 45%), ${theme.colors.alpha.trueWhite[5]}`
            : `radial-gradient(circle at 15% 0%, ${theme.colors.primary.lighter}, transparent 45%), ${theme.colors.alpha.black[5]}`,
          boxShadow: `inset 0 1px 0 ${theme.colors.alpha.trueWhite[10]}`,
        }}
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
              variant={u.isMobile ? "h5" : "h6"}
            sx={{
              paddingLeft: u.isMobile ? 0 : 5,
              width: "100%"
            }}>
            <AutoAwesomeIcon sx={{ mr: 0.5, verticalAlign: "middle", color: theme.palette.warning.main }} />
            {u.t("lottery:draw_available_tickets_total_bet")}
            <strong>{totalBet / 100} {CurrencySymbolsMap[currency]}</strong>
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
