# AGENTS Guide: Defender.UserManagementService

## Purpose
- Owns user profile and account-facing user metadata.

## Project entry points
- Solution: `Defender.UserManagementService.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to change code
- API endpoints: `src/WebApi/Controllers/V1/`
- Business module: `src/Application/Modules/Users/`
- External integrations: `src/Infrastructure/Clients/Identity/`
- Data access: `src/Infrastructure/Repositories/UsersInfo/UserInfoRepository.cs`

## Main controllers
- `UserController`
- `HomeController`

## Dependencies and data
- Uses MongoDB.
- Integrates with Identity service wrapper.

## Fast task playbook
- Add profile endpoint: `WebApi/Controllers/V1/UserController.cs` + command/query in `Application/Modules/Users/`.
- Change user persistence: update `Infrastructure/Repositories/UsersInfo/`.
- Adjust identity-linked behavior: update `Infrastructure/Clients/Identity/IdentityWrapper`.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.UserManagementService.sln`
- Test: `dotnet test Defender.UserManagementService.sln`
