# Defender Portal MCP

Remote Streamable HTTP MCP server for Defender Portal. It calls only the Portal BFF; it never contacts internal Defender services directly.

## Connect as a Portal user

You do not install this server or create an API key. Add its public URL to an MCP client that supports **Streamable HTTP**, OAuth 2.1, dynamic client registration, and PKCE:

```
https://mcp.coded-by-danil.dev/mcp
```

The client opens Defender Portal during its first connection. Sign in with your existing Portal account or create one at [Portal login](https://portal.coded-by-danil.dev/welcome/login), review the requested access, and approve it. The MCP client receives an OAuth token; it never receives your Portal password.

### Codex desktop app

1. Open **Settings** > **MCP servers** > **Add server**.
2. Name it `defender-portal`.
3. Select **Streamable HTTP** and enter `https://mcp.coded-by-danil.dev/mcp`.
4. Save, restart Codex when it asks, then select **Authenticate** next to `defender-portal`.
5. Sign in to Portal, approve access, and return to Codex.
6. Ask a read-only question first, for example: `Show my travel calendar.`

### Codex CLI

```powershell
codex mcp add defender-portal --url https://mcp.coded-by-danil.dev/mcp
codex mcp login defender-portal
codex mcp list
```

`codex mcp list` must show `defender-portal` as enabled and logged in. Then start Codex and ask a read-only question such as `Show my travel calendar.`

### Other MCP clients

Add a remote Streamable HTTP server with the URL above. On first use, follow the client's OAuth browser window. If it cannot open that flow, the client must support OAuth Authorization Code with PKCE and dynamic client registration; no static token is needed.

### What access means

- `mcp:portal:read`: read your Calendar, wallet, and lottery data.
- `mcp:calendar:write`: change your Travel Calendar.
- `mcp:calendar:delete`: delete Calendar data. Delete operations require explicit confirmation.

Access is tied to the Portal account that approved it. Another person must connect and sign in with their own account; they cannot read or change your data. You can reconnect or reauthenticate if a client reports `401` or no longer lists the server as logged in.

### First-use checklist

1. Confirm `https://mcp.coded-by-danil.dev/health` opens successfully.
2. Add the MCP URL in your client.
3. Complete Portal login and consent in the browser window.
4. Run one read-only Calendar request.
5. Only then allow a Calendar change; review any client confirmation before approving it.

## Security model

- Portal is the OAuth authorization server and publishes JWKS.
- MCP validates the public `Host` header before serving any route, including Kubernetes health probes.
- External clients use Authorization Code + PKCE and dynamic client registration.
- Any person can connect a compatible MCP client: it discovers Portal OAuth, registers a public PKCE client, redirects to Portal login, then receives access only to that person's Portal data after consent.
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
