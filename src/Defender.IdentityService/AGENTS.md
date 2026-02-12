# AGENTS Guide: Defender.IdentityService

## Purpose
- AuthN/AuthZ service.
- Manages accounts, access codes, login history, and token flow.

## Project entry points
- Solution: `Defender.IdentityService.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to change code
- API endpoints: `src/WebApi/Controllers/V1/`
- Business modules: `src/Application/Modules/`
  - `AccessCode`, `Account`
- External integrations: `src/Infrastructure/Clients/`
  - `Google`, `Notification`, `UserManagement`
- Data access: `src/Infrastructure/Repositories/`
  - `AccessCodeRepository`, `AccountInfoRepository`, `LoginRecordRepository`

## Main controllers
- `AccessCodeController`
- `AccountController`
- `HomeController`

## Dependencies and data
- Uses MongoDB.
- Calls NotificationService and UserManagementService.
- Supports Google-based auth integration.

## Fast task playbook
- Add account/auth endpoint: controller in `WebApi/Controllers/V1/`, command/query in `Application/Modules/Account/`.
- Change access-code logic: update `Application/Modules/AccessCode/` and related repository.
- Update external auth behavior: adjust `Infrastructure/Clients/Google/`.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.IdentityService.sln`
- Test: `dotnet test Defender.IdentityService.sln`
