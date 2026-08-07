# Defender Health Care Service

Health Care stores 30-minute health events in MongoDB and exposes authenticated event management plus public read-only chart shares. The Portal BFF forwards these contracts to `/api/healthCare/...`.

## API surface

Authenticated endpoints:

- `GET /api/health-events?from={iso}&to={iso}` — list inclusive events for the current user.
- `GET /api/health-events/medication-options` — distinct medication values.
- `POST /api/health-events`, `PUT /api/health-events/{id}`, `DELETE /api/health-events/{id}` — manage events.
- `POST /api/health-chart-shares` — create or update the current user’s stable-token share.
- `GET /api/health-chart-shares/current` — read the current share metadata.
- `PUT /api/health-chart-shares/status` — pause or resume public access.

Public endpoint:

- `GET /api/public/health-chart-shares/{token}` — read an enabled share and its already-authorized events.

## Share range semantics

`HealthChartShareRequest.RangeMode` is a string enum:

- `Rolling` requires both bounds and preserves their duration while moving the window to the current time on each public refresh.
- `Absolute` requires both bounds and keeps the exact inclusive interval selected by the owner.
- `All` requires both bounds to be omitted and returns an unbounded event query.

The persisted `RangeMode` is nullable for backward compatibility. Existing documents with both bounds are inferred as `Rolling`, documents with no bounds as `All`, and one-sided legacy documents retain their fixed bound as `Absolute`.

All `from` and `to` comparisons are inclusive. The Portal shared page may narrow the event array returned by the public endpoint, but it cannot widen the server-authorized range.

## Health checks

The service uses the repository’s common health-check mapping registered by `Program.cs`. Deployment probes should target the service health and readiness endpoints configured by the hosting manifests.
