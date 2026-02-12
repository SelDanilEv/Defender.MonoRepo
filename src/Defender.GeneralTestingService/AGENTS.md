# AGENTS Guide: Defender.GeneralTestingService

## Purpose
- Hosts end-to-end and scenario-driven testing utilities.

## Project entry points
- Solution: `Defender.GeneralTestingService.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to change code
- API endpoints: `src/WebApi/Controllers/V1/`
- Test scenario orchestration: `src/Application/Steps/`
- Test runner services: `src/Application/Services/`
- External integration: `src/Infrastructure/Clients/Portal/PortalWrapper.cs`
- Data access: `src/Infrastructure/Repositories/DomainModelRepository.cs`

## Main controllers
- `TestController`
- `HomeController`

## Dependencies and data
- Uses MongoDB.
- Calls Defender.Portal for integration-style flows.

## Fast task playbook
- Add new test scenario: create/update step in `Application/Steps/` and wire it in service orchestration.
- Expose new test trigger endpoint: `WebApi/Controllers/V1/TestController.cs`.
- Update portal interaction behavior: `Infrastructure/Clients/Portal/`.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.GeneralTestingService.sln`
- Test: `dotnet test Defender.GeneralTestingService.sln`
