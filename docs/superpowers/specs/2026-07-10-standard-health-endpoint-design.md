# Standard Health Endpoint Design

## Goal

Expose a single anonymous `GET /health` endpoint from every ASP.NET service. Remove the controller-based `GET /api/home/health` endpoint completely.

## Design

`Defender.Common` will own two public extension methods:

- `AddDefenderHealthChecks(this IServiceCollection services)` registers ASP.NET Core's built-in health-check services.
- `MapDefenderHealthChecks(this IEndpointRouteBuilder endpoints)` maps the standard endpoint at `/health` with a shared JSON response writer.

Each service and the service template will call the registration method before building the application and the mapping method after controller routes are mapped. The shared extension will require the ASP.NET Core shared framework through a `FrameworkReference` so it can expose the endpoint-mapping API without duplicating code in every service.

Health checking is infrastructure behavior, not a controller API. Therefore every `HomeController.HealthCheckAsync` action, its MediatR query dependency, and controller tests will be removed. Other Home controller actions retain their current API routes. Existing generated health clients are retained because Portal's KeepAliveHostedService uses them to keep synchronous dependencies warm; their NSwag operation paths move to `/health`. The shared response writer returns the existing JSON status shape so those clients remain compatible.

The Helm service-template default probe path will change to `/health`. Existing values files inherit that default, so Argo CD Application manifests need no route-specific change unless the inventory finds an explicit override.

## Verification

- Add shared-library tests for registration and `/health` endpoint mapping, first observed failing.
- Build and test `Defender.Common`, plus service projects affected by startup changes.
- Scan source, Helm, and scripts to confirm no `api/home/health` remains.
- Render the Helm template and verify startup, readiness, and liveness probes use `/health`.

## Scope

The standard endpoint reports built-in health-check status only. No dependency-specific checks are added in this migration; those can be registered later through the shared health-check builder when requirements are defined.
