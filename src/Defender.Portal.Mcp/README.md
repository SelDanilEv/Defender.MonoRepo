# Defender Portal MCP

Remote Streamable HTTP MCP server for Defender Portal. It calls only the Portal BFF; it never contacts internal Defender services directly.

## Security model

- Portal is the OAuth authorization server and publishes JWKS.
- External clients use Authorization Code + PKCE and dynamic client registration.
- The MCP server validates Portal tokens for `defender-mcp`, then exchanges them at Portal for a 60-second BFF token.
- `calendar_mutate` exposes only the documented Travel Calendar routes. Deletes require `confirm: true` and the `mcp:calendar:delete` scope.

## Local verification

```powershell
npm ci
npm run typecheck
npm run build
node dist/index.js
```

The service provides `/health`, `/mcp`, and `/.well-known/oauth-protected-resource/mcp`.
