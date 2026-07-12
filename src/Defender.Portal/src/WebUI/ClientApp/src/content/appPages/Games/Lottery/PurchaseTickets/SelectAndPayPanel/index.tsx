import { Card, Divider, Grid, MenuItem } from "@mui/material";
import { useState } from "react";
import { connect } from "react-redux";
import useUtils from "src/appUtils";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";
import LotteryDraw from "src/models/games/lottery/LotteryDraw";
import LockedTextField from "src/components/LockedComponents/LockedTextField/LockedTextField";
import ParamsObjectBuilder from "src/helpers/ParamsObjectBuilder";
import PurchaseLotteryTicketsRequest from "src/models/requests/games/lottery/PurchaseLotteryTicketsRequest";
import { PositiveCurrencyAmountMaskRegex } from "src/consts/Regexes";
import LockedSelect from "src/components/LockedComponents/LockedSelect/LockedSelect";
import TicketSelection from "./TicketSelection";

interface SelectAndPayPanelProps {
  draw: LotteryDraw;
}

const SelectAndPayPanel = (props: SelectAndPayPanelProps) => {
  const u = useUtils();

  const draw = props.draw;

  const [selectedTickets, setSelectedTickets] = useState<number[]>([]);

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
    <Card sx={{ m: 1, p: 1 }}>
      <Grid container spacing={2}>
        <Grid
          container
          size={{
            xs: 0,
            sm: 1
          }}></Grid>
        <Grid
          container
          sx={{
            gap: 1
          }}
          size={{
            xs: 6,
            sm: 5
          }}>
          {renderPossibleBets()}
        </Grid>
        <Grid
          container
          sx={{
            justifyContent: "right"
          }}
          size={{
            xs: 2.8,
            sm: 4
          }}>
          <LockedTextField
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
            xs: 3.2,
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
          size={{
            xs: 12,
            sm: 12
          }}>
          <Divider />
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
