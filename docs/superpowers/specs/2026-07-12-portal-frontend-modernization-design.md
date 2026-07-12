# Portal Frontend Modernization Design

## Goal

Fix all five frontend audit findings, upgrade React, Material UI, and every npm dependency to current stable releases, retain existing Portal behavior, and deliver one verified commit on `main`.

## Architecture

Keep existing route and feature boundaries. Modernize platform foundations in place: React root/bootstrap, MUI theme and components, TypeScript/Vite/Vitest/ESLint configuration, a typed centralized HTTP client, and CI gates. Avoid business redesign.

## Workstreams

1. Upgrade all dependencies and restore strict TypeScript correctness.
2. Add deterministic frontend CI gates and reproducible npm publishing.
3. Centralize API errors, session expiry, timeouts, and cancellation.
4. Eliminate every React hook warning without stale closures or render loops.
5. Add critical journey, route, auth-expiry, API, and accessibility smoke coverage.

## Compatibility Requirements

- Existing routes, BFF endpoint contracts, localization keys, responsive layouts, and role guards remain compatible.
- Google login remains operational with current OAuth library behavior.
- No direct MUI v4 or deprecated `@mui/styles` use remains.
- One date library remains where practical; migrations must preserve date semantics.
- Production dependencies have no known audit findings at configured CI threshold.
- `npm run typecheck`, `npm run lint`, `npm test`, and `npm run build` all pass with zero warnings/errors.

## Delivery

Five implementation agents, one per audit point. Agents do not commit. Primary agent integrates, reviews, verifies, and creates one commit.
