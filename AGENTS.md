# Repository Guidelines

## Project Structure & Module Organization
- `src/` contains production microservices and shared libraries (`Defender.Common`, `Defender.Kafka`, `Defender.DistributedCache`).
- Each service follows layered structure under `src/Defender.<Service>/src/`: `Application`, `Domain`, `Infrastructure`, and `WebApi`/`WebUI`.
- Service tests live next to code in `*.Tests` projects (example: `src/Defender.PersonalFoodAdvisor/src/Defender.PersonalFoodAdvisor.Tests`).
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
- Compact Portal verification: `powershell -NoProfile -File scripts/verify-portal.ps1` (`-IncludeE2E` for browser journeys, `-TestPath <path>` for one test file).
- Guarded Portal deployment: preview with `powershell -NoProfile -File scripts/deploy-portal.ps1`; after explicit user approval, mutate GitHub/ArgoCD with `powershell -NoProfile -File scripts/deploy-portal.ps1 -Execute`.

## Token-Efficient Agent Workflow
- For Portal work, use `scripts/verify-portal.ps1` instead of streaming raw npm command output. Successful runs should be reported from its compact summary; expand only the failing step's bounded log tail.
- Do not push `main` when it triggers image publication, dispatch build/publish workflows, promote images, mutate GitHub/ArgoCD, inspect production runtime state, or run public production smoke tests by default. Before any of those steps, ask for and receive explicit user approval in the current task; a request to implement a change, commit it, or use `main` is not approval.
- Use `scripts/deploy-portal.ps1` for Portal build/promotion/live verification only after explicit user approval. Always run preview first; `-Execute` is required for external writes.
- Read this file and only the nearest relevant `AGENTS.md`/README files. Avoid re-reading unrelated service documentation.
- During long GitHub/Argo waits, report state transitions only. Do not repeat unchanged status or raw workflow logs.
- Final reports should contain test counts, build/audit status, workflow IDs, deployed image, and live HTTP status—not full command output.

## Coding Style & Naming Conventions
- Follow `src/.editorconfig`: LF line endings, final newline, 4-space indentation for C#.
- Use block-scoped namespaces and place `using` directives outside namespaces.
- Prefer `var` in most cases.
- Naming: interfaces start with `I`; types/methods/properties use `PascalCase`; keep namespaces aligned with folder paths.
- Frontend: reuse existing shared components whenever possible and keep a single standard implementation for each UI element type across the app. Only introduce a second variant when the user explicitly asks for it.
- Frontend: for React UI work in `Defender.Portal`, use Material UI (`@mui/material`, `@mui/icons-material`) as the default component library and styling base unless the user explicitly asks for something else.

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
