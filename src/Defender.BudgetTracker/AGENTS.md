# AGENTS Guide: Defender.BudgetTracker

## Purpose
- Tracks user budget positions, groups, reviews, and diagram setup.

## Project entry points
- Solution: `Defender.BudgetTracker.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to change code
- API endpoints: `src/WebApi/Controllers/V1/`
- Business modules: `src/Application/Modules/`
  - `BudgetReviews`, `DiagramSetups`, `Groups`, `Positions`
- External clients: `src/Infrastructure/Clients/ExchangeRatesApi/`
- Data access: `src/Infrastructure/Repositories/`
  - `BudgetReviewRepository`, `DiagramSetupRepository`, `GroupRepository`, `HistoricalExchangeRatesRepository`, `PositionRepository`

## Main controllers
- `BudgetReviewController`
- `DiagramSetupController`
- `GroupController`
- `PositionController`
- `HomeController`

## Dependencies and data
- Uses MongoDB.
- Integrates with exchange rate API client.

## Fast task playbook
- Add/modify review logic: `Application/Modules/BudgetReviews/`.
- Update position/group behavior: `Application/Modules/Positions/` or `Groups/`.
- Change exchange-rate interactions: `Infrastructure/Clients/ExchangeRatesApi/`.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.BudgetTracker.sln`
- Test: `dotnet test Defender.BudgetTracker.sln`
