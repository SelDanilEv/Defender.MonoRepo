# AGENTS Guide: service-template

## Purpose
- Boilerplate service template for creating new Defender microservices.

## Project entry points
- Solution: `Defender.ServiceTemplate.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to copy/modify first
- API layer: `src/WebApi/Controllers/V1/`
- Application module example: `src/Application/Modules/Module/`
- Repository example: `src/Infrastructure/Repositories/DomainModelRepository.cs`
- External wrapper example: `src/Infrastructure/Clients/Service/ServiceWrapper.cs`

## Fast task playbook
- Bootstrap new service: clone this structure and rename namespace/project identifiers.
- Create new feature: add controller + command/query handler + repository implementation.
- Wire dependencies: update `ConfigureServices.cs` in Application/Infrastructure/WebApi.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.ServiceTemplate.sln`
- Test: `dotnet test Defender.ServiceTemplate.sln`
