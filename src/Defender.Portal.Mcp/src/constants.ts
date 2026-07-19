export const PortalMcpScopes = {
  Read: "mcp:portal:read",
  CalendarWrite: "mcp:calendar:write",
  CalendarDelete: "mcp:calendar:delete",
} as const;

export const PortalReadResources = {
  wallet_info: "/api/banking/wallet/info",
  transaction_history: "/api/banking/transaction/history",
  active_lottery_draws: "/api/lottery/draw/active",
  lottery_tickets: "/api/lottery/tickets",
  available_lottery_tickets: "/api/lottery/tickets/available",
} as const;

export type PortalReadResource = keyof typeof PortalReadResources;
