import ConfirmationNumberIcon from "@mui/icons-material/ConfirmationNumber";
import { Card, Chip, Divider, Grid, MenuItem, Typography } from "@mui/material";
import { useEffect, useRef, useState } from "react";
import { connect } from "react-redux";
import useUtils from "src/appUtils";
import APICallWrapper from "src/api/APIWrapper/APICallWrapper";
import apiUrls from "src/api/apiUrls";
import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import LotteryDraw from "src/models/games/lottery/LotteryDraw";
import LockedTextField from "src/components/LockedComponents/LockedTextField/LockedTextField";
import ParamsObjectBuilder from "src/helpers/ParamsObjectBuilder";
import { WalletInfo } from "src/models/banking/WalletInfo";
import PurchaseLotteryTicketsRequest from "src/models/requests/games/lottery/PurchaseLotteryTicketsRequest";
import { PositiveCurrencyAmountMaskRegex } from "src/consts/Regexes";
import LockedSelect from "src/components/LockedComponents/LockedSelect/LockedSelect";
import { getCurrencyAccountBalance } from "./currencyBalance";
import TicketSelection from "./TicketSelection";

interface SelectAndPayPanelProps {
  draw: LotteryDraw;
}

const SelectAndPayPanel = (props: SelectAndPayPanelProps) => {
  const u = useUtils();
  const theme = u.react.theme;
  const utilsRef = useRef(u);
  utilsRef.current = u;

  const draw = props.draw;

  const [selectedTickets, setSelectedTickets] = useState<number[]>([]);
  const [walletInfo, setWalletInfo] = useState<WalletInfo>();

  useEffect(() => {
    let isMounted = true;

    APICallWrapper({
      url: `${apiUrls.banking.walletInfo}`,
      options: {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
        cache: "default",
      },
      utils: utilsRef.current,
      onSuccess: async (response) => {
        const walletInfo: WalletInfo = await response.json();

        if (isMounted) {
          setWalletInfo(walletInfo);
        }
      },
      showError: false,
    });

    return () => {
      isMounted = false;
    };
  }, []);

  const selectTicket = (ticket: number) => {
    setSelectedTickets([...selectedTickets, ticket]);
  };

  const unselectTicket = (ticket: number) => {
    setSelectedTickets(selectedTickets.filter((t) => t !== ticket));
  };

  const [purchaseTicketsRequest, setPurchaseTicketsRequest] =
    useState<PurchaseLotteryTicketsRequest>({
      drawNumber: draw.drawNumber,
      amount:
        draw.allowedBets.length > 0
          ? draw.allowedBets[0] / 100
          : draw.maxBetValue / 100,
      currency: draw.allowedCurrencies[0],
      ticketNumbers: [],
    });

  const purchaseParams = ParamsObjectBuilder.Build(u, purchaseTicketsRequest);
  const selectedCurrencyBalance = getCurrencyAccountBalance(
    purchaseTicketsRequest.currency,
    walletInfo?.currencyAccounts
  );

  const handleUpdateRequest = (event) => {
    const { name, type } = event.target;
    const value =
      type === "checkbox" ? event.target.checked : event.target.value;

    setPurchaseTicketsRequest((prevState) => {
      if (
        name === purchaseParams.amount &&
        value !== "" &&
        (value * 100 < draw.minBetValue ||
          value * 100 > draw.maxBetValue ||
          !PositiveCurrencyAmountMaskRegex.test(value))
      ) {
        return prevState;
      }

      return { ...prevState, [name]: value };
    });
  };

  const renderPossibleBets = () => {
    return draw.allowedBets.map((bet, index) => {
      const betValue = bet / 100;
      const isActive = betValue === +purchaseTicketsRequest.amount;

      return (
        <Grid
          key={betValue}
          container
          sx={{
            justifyContent: "center"
          }}
          size={{
            xs: 3,
            sm: 2,
            md: 2
          }}>
          <LockedButton
            style={{ minWidth: 10 }}
            variant={"contained"}
            color={isActive ? "info" : "primary"}
            onClick={() => {
              setPurchaseTicketsRequest((prevState) => {
                return { ...prevState, amount: betValue };
              });
            }}
            key={index}
          >
            {betValue}
          </LockedButton>
        </Grid>
      );
    });
  };

  return (
    <Card
      sx={{
        m: 0,
        p: { xs: 1.5, md: 2 },
        border: `1px solid ${theme.colors.primary.lighter}`,
        background: `radial-gradient(circle at 0% 0%, ${theme.colors.primary.lighter}, transparent 30%), ${theme.palette.background.paper}`,
      }}
    >
      <Grid container spacing={2}>
        <Grid size={12}>
          <Grid container spacing={1} sx={{ alignItems: "center" }}>
            <Grid size={{ xs: 12, sm: 7 }}>
              <Typography variant="overline" sx={{ color: theme.palette.primary.main, fontWeight: 800 }}>
                {u.t("lottery:draw_entry_kicker")}
              </Typography>
              <Typography variant="h4" sx={{ fontWeight: 900, lineHeight: 1.05 }}>
                {u.t("lottery:draw_entry_stake")}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {u.t("lottery:draw_entry_ticket_hint")}
              </Typography>
            </Grid>
            <Grid
              size={{ xs: 12, sm: 5 }}
              container
              spacing={1}
              sx={{ justifyContent: { xs: "flex-start", sm: "flex-end" } }}
            >
              <Grid size={{ xs: 6, sm: 12 }}>
                <Chip
                  icon={<ConfirmationNumberIcon />}
                  label={
                    selectedTickets.length > 0
                      ? `${selectedTickets.length} ${u.t("lottery:draw_entry_selected")}`
                      : u.t("lottery:draw_entry_selected_zero")
                  }
                  sx={{
                    maxWidth: "100%",
                    color: theme.palette.primary.main,
                    backgroundColor: theme.colors.primary.lighter,
                    "& .MuiChip-icon": { color: theme.palette.primary.main },
                  }}
                />
              </Grid>
              <Grid size={{ xs: 6, sm: 12 }} sx={{ textAlign: { xs: "left", sm: "right" } }}>
                <Typography variant="caption" color="text.secondary">
                  {u.t("lottery:draw_available_tickets_balance")}
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 900 }}>
                  {selectedCurrencyBalance === undefined
                    ? "—"
                    : `${selectedCurrencyBalance / 100} ${CurrencySymbolsMap[purchaseTicketsRequest.currency]}`}
                </Typography>
              </Grid>
            </Grid>
          </Grid>
        </Grid>
        <Grid size={12}>
          <Divider />
        </Grid>
        <Grid
          container
          sx={{
            gap: 1,
            alignItems: "center",
          }}
          size={{
            xs: 12,
            sm: 6
          }}>
          {renderPossibleBets()}
        </Grid>
        <Grid
          container
          sx={{
            justifyContent: "right",
          }}
          size={{
            xs: 7,
            sm: 4
          }}>
          <LockedTextField
            label={u.t("lottery:draw_entry_stake")}
            name={purchaseParams.amount}
            value={purchaseTicketsRequest.amount || ""}
            onChange={handleUpdateRequest}
            type="number"
          />
        </Grid>
        <Grid
          container
          sx={{
            justifyContent: "right"
          }}
          size={{
            xs: 5,
            sm: 2
          }}>
          <LockedSelect
            name={purchaseParams.currency}
            value={purchaseTicketsRequest.currency}
            onChange={handleUpdateRequest}
            fullWidth
          >
            {draw.allowedCurrencies.map((currency) => (
              <MenuItem key={currency} value={currency}>
                {currency}
              </MenuItem>
            ))}
          </LockedSelect>
        </Grid>
        <Grid
          container
          spacing={1}
          sx={{
            marginLeft: 0.5
          }}
          size={{
            xs: 12,
            sm: 12
          }}>
          <TicketSelection
            drawNumber={draw.drawNumber}
            selectedTickets={selectedTickets}
            currency={purchaseTicketsRequest.currency}
            betAmount={purchaseTicketsRequest.amount}
            selectTicket={selectTicket}
            unselectTicket={unselectTicket}
          />
        </Grid>
      </Grid>
    </Card>
  );
};

const mapStateToProps = (state: any) => {
  return {
    currentLanguage: state.session.language,
  };
};

export default connect(mapStateToProps)(SelectAndPayPanel);
