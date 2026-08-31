import ConfirmationNumberIcon from "@mui/icons-material/ConfirmationNumber";
import EmojiEventsIcon from "@mui/icons-material/EmojiEvents";
import { Box, Card, Chip, Grid, Typography } from "@mui/material";
import { connect } from "react-redux";

import useUtils from "src/appUtils";
import CurrencySymbolsMap from "src/consts/CurrencySymbolsMap";
import { mapTicketStatusColor } from "src/mappers/games/lottery/mapTicketStatus";
import mapTicketStatus from "src/mappers/games/lottery/mapTicketStatus";
import LotteryTicket from "src/models/games/lottery/LotteryTicket";

interface LatestTicketsProps {
  LatestTickets: LotteryTicket[];
}

const LatestTickets = (props: LatestTicketsProps) => {
  const u = useUtils();
  const theme = u.react.theme;

  return (
    <Card
      sx={{
        height: "100%",
        p: { xs: 1.5, md: 2 },
        border: `1px solid ${theme.colors.info.lighter}`,
        background: `radial-gradient(circle at 100% 0%, ${theme.colors.info.lighter}, transparent 35%), ${theme.palette.background.paper}`,
      }}
    >
      <Grid container spacing={1.5}>
        <Grid size={{ xs: 12 }} container spacing={1} sx={{ alignItems: "center" }}>
          <Grid size="auto">
            <Box
              data-testid="lottery-latest-tickets-heading-icon"
              sx={{
                width: 40,
                height: 40,
                borderRadius: 2,
                display: "grid",
                placeItems: "center",
                color: theme.palette.warning.contrastText,
                background: theme.colors.gradients.orange1,
                boxShadow: theme.colors.shadows.warning,
              }}
            >
              <EmojiEventsIcon />
            </Box>
          </Grid>
          <Grid size="grow" sx={{ minWidth: 0 }}>
            <Typography variant="overline" sx={{ color: theme.palette.warning.dark, fontWeight: 800 }}>
              {u.t("lottery:arena_ticket_board_kicker")}
            </Typography>
            <Typography variant="h4" sx={{ fontWeight: 900, lineHeight: 1.05 }}>
              {u.t("lottery:latest_tickets_title")}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {u.t("lottery:arena_ticket_board_subtitle")}
            </Typography>
          </Grid>
        </Grid>

        {props.LatestTickets.length > 0 ? (
          <Grid size={{ xs: 12 }} container spacing={1}>
            {props.LatestTickets.map((ticket, index) => {
              const statusColor = mapTicketStatusColor(u, ticket.status);

              return (
                <Grid key={`${ticket.drawNumber}-${ticket.ticketNumber}-${index}`} size={{ xs: 12 }}>
                  <Card
                    sx={{
                      p: 1.25,
                      borderLeft: `3px solid ${theme.palette.primary.main}`,
                      backgroundColor: statusColor,
                      boxShadow: "none",
                    }}
                  >
                    <Grid container spacing={1} sx={{ alignItems: "center" }}>
                      <Grid size={{ xs: 2 }} sx={{ minWidth: 0 }}>
                        <Box
                          sx={{
                            width: 30,
                            height: 30,
                            borderRadius: "50%",
                            display: "grid",
                            placeItems: "center",
                            color: theme.palette.primary.main,
                            backgroundColor: theme.colors.primary.lighter,
                          }}
                        >
                          <ConfirmationNumberIcon sx={{ fontSize: 17 }} />
                        </Box>
                      </Grid>
                      <Grid size={{ xs: 6 }} sx={{ minWidth: 0 }}>
                        <Typography variant="caption" color="text.secondary">
                          #{ticket.drawNumber}
                        </Typography>
                        <Typography variant="h6" sx={{ fontWeight: 900, lineHeight: 1.1 }}>
                          {ticket.ticketNumber}
                        </Typography>
                      </Grid>
                      <Grid size={{ xs: 4 }} sx={{ minWidth: 0, textAlign: "right" }}>
                        <Chip
                          data-testid="lottery-ticket-status"
                          label={mapTicketStatus(u, ticket.status)}
                          size="small"
                          sx={{
                            maxWidth: "100%",
                            minWidth: 0,
                            color:
                              theme.palette.mode === "dark"
                                ? theme.colors.alpha.white[100]
                                : theme.palette.text.primary,
                            backgroundColor: theme.colors.alpha.trueWhite[70],
                            "& .MuiChip-label": {
                              overflow: "hidden",
                              textOverflow: "ellipsis",
                            },
                          }}
                        />
                        <Typography variant="caption" sx={{ display: "block" }} color="text.secondary">
                          {ticket.amount / 100}
                          {CurrencySymbolsMap[ticket.currency]}
                        </Typography>
                      </Grid>
                    </Grid>
                  </Card>
                </Grid>
              );
            })}
          </Grid>
        ) : (
          <Grid size={{ xs: 12 }}>
            <Box
              sx={{
                minHeight: 230,
                px: 2,
                py: 3,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                textAlign: "center",
                borderRadius: theme.general.borderRadiusLg,
                border: `1px dashed ${theme.colors.info.light}`,
                backgroundColor: theme.colors.info.lighter,
              }}
            >
              <ConfirmationNumberIcon sx={{ mb: 1, fontSize: 34, color: theme.palette.info.main }} />
              <Typography variant="h6" sx={{ fontWeight: 800 }}>
                {u.t("lottery:arena_no_tickets_title")}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {u.t("lottery:arena_no_tickets_description")}
              </Typography>
            </Box>
          </Grid>
        )}
      </Grid>
    </Card>
  );
};

const mapStateToProps = (state: any) => {
  return {};
};

export default connect(mapStateToProps)(LatestTickets);
