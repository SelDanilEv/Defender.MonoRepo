# Defender Portal MCP

Remote Streamable HTTP MCP server for Defender Portal. It calls only the Portal BFF; it never contacts internal Defender services directly.

## Security model

- Portal is the OAuth authorization server and publishes JWKS.
- MCP validates the public `Host` header before serving any route, including Kubernetes health probes.
- External clients use Authorization Code + PKCE and dynamic client registration.
- The MCP server validates Portal tokens for `defender-mcp`, then exchanges them at Portal for a 60-second BFF token.
- Tools use `defender_portal_*` names, match Portal controller routes, and return structured JSON data.
- `defender_portal_calendar_mutate` covers every mutation in `TravelCalendarController`. Deletes require `confirm: true` and the `mcp:calendar:delete` scope.

## Tools

- `defender_portal_calendar_get` and `defender_portal_calendar_search_users` read Calendar data.
- `defender_portal_calendar_mutate` supports all Calendar mutations: theme, queued trips, events, auto-scheduling, points, participants, and packing items. Its operation names mirror `TravelCalendarController`.
- `defender_portal_read` exposes only current read routes from `BankingController` and `LotteryController`: wallet info, transaction history, active draws, user tickets, and available tickets.

All write tools call the Portal BFF. Request `body` fields must match the corresponding Portal request model, including `expectedVersion` for Calendar optimistic concurrency.

## Local verification

```powershell
npm ci
npm test
npm run typecheck
npm run build
node dist/index.js
```

The service provides `/health`, `/mcp`, and `/.well-known/oauth-protected-resource/mcp`.
