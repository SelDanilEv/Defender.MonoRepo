# AGENTS Guide: Defender.JobSchedulerService

## Purpose
- Schedules and triggers background jobs.

## Project entry points
- Solution: `Defender.JobSchedulerService.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to change code
- API endpoints: `src/WebApi/Controllers/V1/`
- Business logic services: `src/Application/Services/`
- Data access: `src/Infrastructure/Repositories/ScheduledJobRepository.cs`

## Main controllers
- `JobManagementController`
- `HomeController`

## Dependencies and data
- Uses MongoDB.
- Uses Kafka to publish/trigger scheduled work.

## Fast task playbook
- Add job management endpoint: `WebApi/Controllers/V1/JobManagementController.cs` + service changes in `Application/Services/`.
- Change scheduling persistence: `Infrastructure/Repositories/ScheduledJobRepository.cs`.
- Adjust event flow: inspect Kafka option wiring in configuration and services.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.JobSchedulerService.sln`
- Test: `dotnet test Defender.JobSchedulerService.sln`
