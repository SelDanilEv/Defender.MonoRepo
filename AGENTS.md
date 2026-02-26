# Repository Guidelines

## Project Structure & Module Organization
- `src/` contains production microservices and shared libraries (`Defender.Common`, `Defender.Kafka`, `Defender.DistributedCache`).
- Each service follows layered structure under `src/Defender.<Service>/src/`: `Application`, `Domain`, `Infrastructure`, and `WebApi`/`WebUI`.
- Service tests live next to code in `*.Tests` projects (example: `src/Defender.PersonalFoodAdviser/src/Defender.PersonalFoodAdviser.Tests`).
- `tools/` contains support apps (for example, secret management and Mongo migration tools).
- `helm/` stores Kubernetes/ArgoCD manifests; `scripts/` contains automation; `secrets/` stores local env-file templates.
- For service-specific guidance, check that service’s local `AGENTS.md` when present.

## Build, Test, and Development Commands
- Restore/build a service:
  - `dotnet restore src/Defender.WalletService/Defender.WalletService.sln`
  - `dotnet build src/Defender.WalletService/Defender.WalletService.sln -c Debug`
- Run tests:
  - `dotnet test src/Defender.WalletService/Defender.WalletService.sln`
  - or target a test project directly with `dotnet test <path-to-*.Tests.csproj>`.
- Run local infrastructure/services via Docker:
  - `docker compose -f src/docker-compose.yml --profile local up -d --build`
  - `docker compose -f src/docker-compose.yml down`
- Portal frontend (if needed): `cd src/Defender.Portal/src/WebUI/ClientApp && npm install && npm start`.

## Coding Style & Naming Conventions
- Follow `src/.editorconfig`: LF line endings, final newline, 4-space indentation for C#.
- Use block-scoped namespaces and place `using` directives outside namespaces.
- Prefer `var` in most cases.
- Naming: interfaces start with `I`; types/methods/properties use `PascalCase`; keep namespaces aligned with folder paths.

## Testing Guidelines
- Primary test stack is xUnit with Moq.
- Name tests with `Method_WhenCondition_ExpectedResult`.
- Add or update tests for behavior changes in Application/Infrastructure layers before opening a PR.
- **Unit test layout**: For each `Defender.<Service>`, keep tests under `src/Defender.<Service>/src/Tests/` with this layout:
  - `Services/` — application service tests
  - `Handlers/` — MediatR command/query handler tests
  - `Validators/` — FluentValidation validator tests
  - `Models/` — request/DTO mapping tests
  - `Domain/` — entity and helper tests
  - `Infrastructure/Clients/` — external API wrapper tests
  - `Infrastructure/Mappings/` — AutoMapper profile tests
  - `Controllers/` — WebApi controller tests
  Use `.cursor/skills/generate-unit-tests/SKILL.md` for full patterns and coverage goals.

## Commit & Pull Request Guidelines
- Use imperative commit subjects (`Add...`, `Update...`, `Fix...`); Conventional Commit prefixes are used for chores/deploy (example: `chore(deploy): ...`).
- Keep commits scoped to one logical change.
- PRs should include: purpose, impacted services, test evidence (`dotnet test`/compose checks), and linked issue/ticket.
- Include request/response examples or screenshots when API contracts or UI behavior change.

## Security & Configuration Tips
- Never commit secrets. Use `secrets/secrets.local.list` or `secrets/secrets.dev.list`.
- Validate environment-specific settings (`appsettings.Local.json`, `appsettings.Dev.json`, etc.) before deployment.
