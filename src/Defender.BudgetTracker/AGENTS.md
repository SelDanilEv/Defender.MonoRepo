# AGENTS Guide: Defender.BudgetTracker

## Purpose
- Tracks user budget positions, groups, reviews, and diagram setup.
- Regular Expenses is independent vertical slice. Do not reuse balance position/review/setup entities or collections.

## Project entry points
- Solution: `Defender.BudgetTracker.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to change code
- API endpoints: `src/WebApi/Controllers/V1/`
- Business modules: `src/Application/Modules/`
  - `BudgetReviews`, `DiagramSetups`, `Groups`, `Positions`
  - `RegularExpenses`, `RegularExpenseReviews`, `RegularExpenseDiagramSetups`
- External clients: `src/Infrastructure/Clients/ExchangeRatesApi/`
- Data access: `src/Infrastructure/Repositories/`
  - `BudgetReviewRepository`, `DiagramSetupRepository`, `GroupRepository`, `HistoricalExchangeRatesRepository`, `PositionRepository`
  - `RegularExpenseRepository`, `RegularExpenseReviewRepository`, `RegularExpenseDiagramSetupRepository`

## Main controllers
- `BudgetReviewController`
- `DiagramSetupController`
- `GroupController`
- `PositionController`
- `HomeController`
- `RegularExpenseController`
- `RegularExpenseReviewController`
- `RegularExpenseDiagramSetupController`

## Dependencies and data
- Uses MongoDB.
- Integrates with exchange rate API client.
- Regular expense definitions, monthly snapshots, and diagram setup use separate MongoDB collections and user-scoped filters.
- Review months normalize to day 1. Annual amounts contribute `amount / 12m`; snapshots capture current definition metadata and review-month rates.

## Fast task playbook
- Add/modify review logic: `Application/Modules/BudgetReviews/`.
- Update position/group behavior: `Application/Modules/Positions/` or `Groups/`.
- Change exchange-rate interactions: `Infrastructure/Clients/ExchangeRatesApi/`.
- Add or modify Regular Expenses behavior: `Application/Modules/RegularExpenses/`, `RegularExpenseReviews/`, or `RegularExpenseDiagramSetups/`.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.BudgetTracker.sln`
- Test: `dotnet test Defender.BudgetTracker.sln`
