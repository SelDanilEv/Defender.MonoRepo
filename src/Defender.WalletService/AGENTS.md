# AGENTS Guide: Defender.WalletService

## Purpose
- Core financial service for wallets, balances, and transactions.

## Project entry points
- Solution: `Defender.WalletService.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to change code
- API endpoints: `src/WebApi/Controllers/V1/`
- Business modules: `src/Application/Modules/`
  - `Transactions`, `Wallets`
- Data access: `src/Infrastructure/Repositories/`
  - `TransactionRepository`, `WalletRepositoryRepository`

## Main controllers
- `TransactionController`
- `WalletController`
- `HomeController`

## Dependencies and data
- Uses MongoDB for domain data.
- Uses Kafka (`Defender.Kafka`) for messaging.
- Uses distributed cache (`Defender.DistributedCache`, Postgres-backed).

## Fast task playbook
- Add transaction behavior: update `Application/Modules/Transactions/` + `Infrastructure/Repositories/TransactionRepository.cs`.
- Add wallet APIs: controller in `WebApi/Controllers/V1/WalletController.cs` + `Application/Modules/Wallets/`.
- Change async/event behavior: inspect Kafka wiring in application/infrastructure options and services.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.WalletService.sln`
- Test: `dotnet test Defender.WalletService.sln`
