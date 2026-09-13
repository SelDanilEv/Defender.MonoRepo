# Defender CarService

Standalone backend for the My Garage feature. This first shell provides the Clean Architecture projects, common platform registration, JWT authentication, MongoDB health readiness, and local development settings. Domain contracts and route handlers are added by later tasks.

## Local commands

```powershell
dotnet restore src/Defender.CarService/Defender.CarService.sln
dotnet build src/Defender.CarService/Defender.CarService.sln -c Debug
dotnet test src/Defender.CarService/Defender.CarService.sln -c Debug
dotnet run --project src/Defender.CarService/src/WebApi/Defender.CarService.WebApi.csproj --launch-profile Local
```

The local HTTP port is `47065`. Health endpoints are `/health` and `/health/ready`.

## Project layout

- `src/Domain` contains domain contracts and rules.
- `src/Application` contains application requests and handlers.
- `src/Infrastructure` contains MongoDB persistence and integrations.
- `src/WebApi` contains HTTP hosting and API configuration.
- `src/Tests` contains service tests.

MongoDB uses `MongoDbOptions` and resolves the local database as `local_Defender_CarService`. Local and Debug configurations use the development JWT key from their environment-specific settings. Dev and Prod require the normal secret provider.
