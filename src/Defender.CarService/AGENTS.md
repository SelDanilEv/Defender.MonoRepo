# CarService Agent Guide

## Scope

Defender.CarService is a user-scoped Clean Architecture service for My Garage. Keep domain rules independent from MongoDB, HTTP, Portal, notifications, schedulers, Kafka, imports, and tax workflows.

## Commands

```powershell
dotnet restore src/Defender.CarService/Defender.CarService.sln
dotnet build src/Defender.CarService/Defender.CarService.sln -c Debug
dotnet test src/Defender.CarService/Defender.CarService.sln -c Debug
```

Use `appsettings.Local.json` with the Local launch profile for local development. The service listens on port `47065` and exposes `/health` and `/health/ready`.

## Rules

- Keep namespaces and assembly names under `Defender.CarService.*`.
- Every data operation must remain scoped to the authenticated user.
- Keep MongoDB transactions enabled through the shared Mongo client and replica set configuration.
- Do not add Portal, Compose, Helm, workflow, root solution, or deployment changes in this service directory task.
- Do not commit secrets or generated `bin` and `obj` output.
