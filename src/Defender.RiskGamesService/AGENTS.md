# AGENTS Guide: Defender.RiskGamesService

## Purpose
- Manages risk-based games (currently lottery).

## Project entry points
- Solution: `Defender.RiskGamesService.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to change code
- API endpoints: `src/WebApi/Controllers/V1/`
  - `V1/Lottery/` contains lottery-specific controllers
- Business modules: `src/Application/Modules/Lottery/`
  - `Commands`, `Queries`, `Tickets`
- Data access: `src/Infrastructure/Repositories/`
  - `Lottery/` and `Transactions/`
- External integrations: `src/Infrastructure/Clients/Wallet/`

## Main controllers
- `LotteryController`
- `LotteryDrawController`
- `UserDataController`
- `HomeController`

## Dependencies and data
- Uses MongoDB.
- Uses Kafka.
- Calls Wallet service for payment/transaction flows.

## Fast task playbook
- Lottery business rules: start in `Application/Modules/Lottery/`.
- Ticket persistence changes: `Infrastructure/Repositories/Lottery/`.
- Payment/integration issues: `Infrastructure/Clients/Wallet/` and relevant command handlers.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.RiskGamesService.sln`
- Test: `dotnet test Defender.RiskGamesService.sln`
