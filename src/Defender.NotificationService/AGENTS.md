# AGENTS Guide: Defender.NotificationService

## Purpose
- Sends and monitors outbound notifications (email-first).

## Project entry points
- Solution: `Defender.NotificationService.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to change code
- API endpoints: `src/WebApi/Controllers/V1/`
- Business modules: `src/Application/Modules/`
  - `Notifications`, `Monitoring`
- External providers: `src/Infrastructure/Clients/SendinBlueClient/`
- Data access: `src/Infrastructure/Repositories/Notifications/NotificationRepository.cs`

## Main controllers
- `NotificationController`
- `MonitoringController`
- `HomeController`

## Dependencies and data
- Uses MongoDB.
- Integrates with SendinBlue provider.

## Fast task playbook
- Add notification endpoint: controller in `WebApi/Controllers/V1/` + command/query in `Application/Modules/Notifications/`.
- Change delivery/provider logic: `Infrastructure/Clients/SendinBlueClient/`.
- Update monitoring behavior: `Application/Modules/Monitoring/`.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.NotificationService.sln`
- Test: `dotnet test Defender.NotificationService.sln`
