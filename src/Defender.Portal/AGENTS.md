# AGENTS Guide: Defender.Portal

## Purpose
- BFF + UI gateway for end-user and admin workflows.
- Hosts WebUI and aggregates calls to backend services.

## Project entry points
- Solution: `Defender.Portal.sln`
- Startup project: `src/WebUI/WebUI.csproj`
- App entry: `src/WebUI/Program.cs`

## Where to change code
- API endpoints (BFF): `src/WebUI/Controllers/V1/`
- Feature orchestration: `src/Application/Modules/`
  - `Accounts`, `Admin`, `Banking`, `BudgetTracking`, `RiskGames`
- Outbound service wrappers: `src/Infrastructure/Clients/`
  - `BudgetTracker`, `Identity`, `RiskGames`, `UserManagement`, `Wallet`
- Persistence helpers: `src/Infrastructure/Repositories/UserActivityRepository.cs`

## Main controllers
- `AccountController`
- `AuthorizationController`
- `BankingController`
- `BudgetTrackerController`
- `LotteryController`
- `VerificationController`
- `AdminUserController`
- `AdminBankingController`

## Dependencies and data
- Depends on: Identity, UserManagement, Wallet, RiskGames, BudgetTracker.
- Uses MongoDB and distributed cache (`Defender.DistributedCache`).

## Fast task playbook
- Add new BFF endpoint: controller in `WebUI/Controllers/V1/` + handler/service in `Application/Modules/`.
- Integrate backend API: add/update client wrapper in `Infrastructure/Clients/` and map DTOs in Application.
- Admin workflow changes: update `Controllers/V1/Admin/` and matching `Application/Modules/Admin/`.

## Commands
- Run: `dotnet run --project src/WebUI/WebUI.csproj`
- Build: `dotnet build Defender.Portal.sln`
- Test: `dotnet test Defender.Portal.sln`
