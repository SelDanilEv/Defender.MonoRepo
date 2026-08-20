import LotteryDraw from "src/models/games/lottery/LotteryDraw";
import LotteryTicket, { TicketStatus } from "src/models/games/lottery/LotteryTicket";

export interface LotteryArenaStats {
  activeDraws: number;
  tickets: number;
  wins: number;
}

export interface LotteryArenaHeroTextColors {
  value: string;
  label: string;
}

export const getLotteryArenaHeroTextColors = (theme: {
  colors: { alpha: { trueWhite: { 70: string; 100: string } } };
}): LotteryArenaHeroTextColors => ({
  value: theme.colors.alpha.trueWhite[100],
  label: theme.colors.alpha.trueWhite[70],
});

const isLotteryDraw = (value: unknown): value is LotteryDraw => {
  if (!value || typeof value !== "object") {
    return false;
  }

  const draw = value as Partial<LotteryDraw>;

  return (
    typeof draw.drawNumber === "number" &&
    !!draw.publicNames &&
    Array.isArray(draw.coefficients) &&
    Array.isArray(draw.allowedBets) &&
    Array.isArray(draw.allowedCurrencies) &&
    typeof draw.minBetValue === "number" &&
    typeof draw.maxBetValue === "number" &&
    typeof draw.endDate !== "undefined"
  );
};

export const readLotteryDrawState = (state: unknown): LotteryDraw | null => {
  if (!state || typeof state !== "object") {
    return null;
  }

  const draw = (state as { draw?: unknown }).draw;
  return isLotteryDraw(draw) ? draw : null;
};

export const getDrawMultiplier = (draw: Pick<LotteryDraw, "coefficients">): string => {
  const multiplier = Math.max(...(draw.coefficients ?? []), 0) / 100;
  return multiplier > 0 ? `x${multiplier}` : "—";
};

export const getLotteryArenaStats = (
  draws: LotteryDraw[],
  tickets: LotteryTicket[],
): LotteryArenaStats => ({
  activeDraws: draws.length,
  tickets: tickets.length,
  wins: tickets.filter(
    (ticket) =>
      ticket.status === TicketStatus.Won || ticket.status === TicketStatus.PrizePaid,
  ).length,
});
