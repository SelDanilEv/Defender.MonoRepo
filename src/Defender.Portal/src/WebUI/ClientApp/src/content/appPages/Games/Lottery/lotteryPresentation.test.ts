import LotteryDraw from "src/models/games/lottery/LotteryDraw";
import LotteryTicket, { TicketStatus } from "src/models/games/lottery/LotteryTicket";
import {
  getDrawMultiplier,
  getLotteryArenaHeroTextColors,
  getLotteryArenaStats,
  getRandomAvailableLotteryTicket,
  getLotteryTicketSelectionProgress,
  readLotteryDrawState,
} from "./lotteryPresentation";

const draw = {
  drawNumber: 12,
  publicNames: { en: "Lucky draw" },
  startDate: new Date("2026-08-19T20:00:00Z"),
  endDate: new Date("2026-08-20T20:00:00Z"),
  coefficients: [125, 250, 700],
  allowedBets: [100],
  allowedCurrencies: ["USD"],
  minBetValue: 100,
  maxBetValue: 100,
  isCustomBetAllowed: false,
  minTicketNumber: 1,
  maxTicketNumber: 999,
  isActive: true,
} as LotteryDraw;

const ticket = (status: TicketStatus) =>
  ({
    drawNumber: draw.drawNumber,
    ticketNumber: 101,
    amount: 100,
    currency: "USD",
    userId: "user-1",
    paymentTransactionId: "payment-1",
    prizeTransactionId: "",
    prizePaidAmount: 0,
    status,
  }) as LotteryTicket;

describe("lotteryPresentation", () => {
  test("readLotteryDrawState_WhenLocationStateIsMissing_ReturnsNull", () => {
    expect(readLotteryDrawState(null)).toBeNull();
  });

  test("readLotteryDrawState_WhenStateContainsDraw_ReturnsDraw", () => {
    expect(readLotteryDrawState({ draw })).toBe(draw);
  });

  test("readLotteryDrawState_WhenStateContainsMalformedDraw_ReturnsNull", () => {
    expect(readLotteryDrawState({ draw: { drawNumber: 12 } })).toBeNull();
  });

  test("getDrawMultiplier_WhenCoefficientsExist_ReturnsLargestCoefficient", () => {
    expect(getDrawMultiplier(draw)).toBe("x7");
  });

  test("getLotteryArenaStats_WhenTicketsContainWins_CountsWinningTickets", () => {
    expect(
      getLotteryArenaStats([draw], [
        ticket(TicketStatus.Paid),
        ticket(TicketStatus.Won),
        ticket(TicketStatus.PrizePaid),
      ]),
    ).toEqual({ activeDraws: 1, tickets: 3, wins: 2 });
  });

  test("getLotteryArenaHeroTextColors_UsesReadableLightTextOnDarkHero", () => {
    expect(
      getLotteryArenaHeroTextColors({
        colors: { alpha: { trueWhite: { 70: "rgba(255,255,255,.7)", 100: "#fff" } } },
      }),
    ).toEqual({ value: "#fff", label: "rgba(255,255,255,.7)" });
  });

  test("getLotteryTicketSelectionProgress_ClampsSelectionToBoardSize", () => {
    expect(getLotteryTicketSelectionProgress(0, 25)).toBe(0);
    expect(getLotteryTicketSelectionProgress(5, 25)).toBe(20);
    expect(getLotteryTicketSelectionProgress(30, 25)).toBe(100);
  });

  test("getRandomAvailableLotteryTicket_SkipsAlreadySelectedTickets", () => {
    expect(getRandomAvailableLotteryTicket([11, 22, 33], [22], 0.99)).toBe(33);
    expect(getRandomAvailableLotteryTicket([11], [11], 0.5)).toBeNull();
  });
});
