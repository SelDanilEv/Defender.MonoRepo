# Defender CarService

Standalone backend for My Garage. Provides Clean Architecture projects, JWT authentication, MongoDB readiness, optional metrics, and the user-owned HTTP API.

## API

Base route: `/api/V1/car`. Every route requires a bearer JWT with the `User` role. Account ownership comes from the authenticated JWT account and is never accepted from request data.

- Vehicles: `GET /vehicles`, `POST /vehicles` (201), `GET /vehicles/{vehicleId}`, `PUT /vehicles/{vehicleId}`, `POST /vehicles/{vehicleId}/archive`, and `POST /vehicles/{vehicleId}/unarchive`.
- Maintenance: `GET` and `POST /vehicles/{vehicleId}/maintenance`, `PUT` and `DELETE /vehicles/{vehicleId}/maintenance/{maintenanceId}` (204 delete).
- History: `GET` and `POST /vehicles/{vehicleId}/history`, `PUT` and `DELETE /vehicles/{vehicleId}/history/{historyId}` (204 delete). `GET` defaults to `page=0&pageSize=25`.
- Insurance: `GET` and `POST /vehicles/{vehicleId}/insurance`, and `PUT /vehicles/{vehicleId}/insurance/{insuranceId}`.

Errors use ProblemDetails with stable `CAR_*` codes and status mapping for validation, ownership-scoped not found, conflicts, database unavailability, and unexpected failures. There is no insurance delete, tax, import, migration, or notification route.

## Local commands

```powershell
dotnet restore src/Defender.CarService/Defender.CarService.sln
dotnet build src/Defender.CarService/Defender.CarService.sln -c Debug
dotnet test src/Defender.CarService/Defender.CarService.sln -c Debug
dotnet run --project src/Defender.CarService/src/WebApi/Defender.CarService.WebApi.csproj --launch-profile Local
```

The local HTTP port is `47065`. Health endpoints are `/health` and `/health/ready`.

## Ownership and delivery

CarService owns user-scoped vehicles, maintenance schedules, service history, and insurance. It
does not own tax, import, migration, notification, scheduler, or insurer integrations. Local
Compose maps `47065:8080`; Dev Compose maps `49065:8080`. Both use the shared service network and
Mongo replica set `rs0`.

Helm values are in `helm/service-template/values-car.yaml`. The release is `car-service` in the
`defender` namespace and publishes `defendersd/defender.car`. Image publication, promotion, and
ArgoCD deployment require explicit approval.

## Project layout

- `src/Domain` contains domain contracts and rules.
- `src/Application` contains application requests and handlers.
- `src/Infrastructure` contains MongoDB persistence and integrations.
- `src/WebApi` contains HTTP hosting and API configuration.
- `src/Tests` contains service tests.

MongoDB uses `MongoDbOptions` and resolves the local database as `local_Defender_CarService`. Local and Debug configurations use the development JWT key from their environment-specific settings. Dev and Prod require the normal secret provider.
