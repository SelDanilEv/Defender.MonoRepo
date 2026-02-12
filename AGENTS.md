# Defender.MonoRepo Agent Navigation

Use this file as a shortcut map. Each service has a local `AGENTS.md` with focused implementation guidance.

## Production and support services (`src/`)
- `src/Defender.Portal/AGENTS.md`
- `src/Defender.IdentityService/AGENTS.md`
- `src/Defender.UserManagementService/AGENTS.md`
- `src/Defender.WalletService/AGENTS.md`
- `src/Defender.RiskGamesService/AGENTS.md`
- `src/Defender.NotificationService/AGENTS.md`
- `src/Defender.BudgetTracker/AGENTS.md`
- `src/Defender.JobSchedulerService/AGENTS.md`
- `src/Defender.GeneralTestingService/AGENTS.md`
- `src/Defender.DistributedCache/AGENTS.md` (shared component)
- `src/Defender.Common/AGENTS.md` (shared component)
- `src/Defender.Kafka/AGENTS.md` (shared component)
- `src/service-template/AGENTS.md` (new service scaffold)

## Tooling services (`tools/`)
- `tools/Defender.SecretManagementService/AGENTS.md`
- `tools/Defender.SimpleMongoMigrator/AGENTS.md`

## Suggested workflow for agents
- Identify target service from task scope.
- Read only that service `AGENTS.md`.
- Open the referenced controller/module/repository paths.
- Implement change and run service-local build/test commands.
