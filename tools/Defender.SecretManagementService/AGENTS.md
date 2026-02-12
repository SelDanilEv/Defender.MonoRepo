# AGENTS Guide: Defender.SecretManagementService

## Purpose
- Manages secrets with API-based CRUD/query operations.

## Project entry points
- Solution: `Defender.SecretManagementService.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to change code
- API endpoints: `src/WebApi/Controllers/V1/`
- Business modules: `src/Application/Modules/Secret/`
  - `Commands`, `Queries`
- Application service: `src/Application/Services/SecretManagementService.cs`
- Data access: `src/Infrastructure/Repositories/SecretRepository.cs`
- Environment secret lists: `secrets/`

## Main controllers
- `SecretController`
- `HomeController`

## Dependencies and data
- Uses shared Defender common package/projects.
- Uses repository-backed secret storage plus environment config files.

## Fast task playbook
- Add new secret operation: command/query under `Application/Modules/Secret/` and expose in `WebApi/Controllers/V1/SecretController.cs`.
- Change storage behavior: update `Infrastructure/Repositories/SecretRepository.cs`.
- Adjust environment-specific secrets: edit files in `secrets/`.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.SecretManagementService.sln`
- Docker local: `docker compose -f docker-compose.yml --profile local up --build`
