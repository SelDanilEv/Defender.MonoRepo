# Defender Development Guide

Use this guide for local development, service scaffolding, and browser smoke tests. See
[`PROJECT-OVERVIEW.md`](./PROJECT-OVERVIEW.md) for platform context and
[`OPERATIONS-GUIDE.md`](./OPERATIONS-GUIDE.md) for deployment and observability.

## Run one service against the Docker stack

Prerequisites: Docker Desktop, repository root as working directory, service host port from
`src/docker-compose.yml`, and required `Defender_App_` secrets. Most services need
`Defender_App_JwtSecret` and `Defender_App_MongoDBConnectionString`; Personal Food Advisor also
needs `Defender_App_HuggingFaceApiKey`.

Start the local stack, then stop only the service being debugged:

```powershell
docker compose -f src/docker-compose.yml --profile local up -d
docker compose -f src/docker-compose.yml --profile local stop <compose-service-name>
```

Run its WebApi launch profile on the same host port. Host processes reach Kafka, MongoDB, and
PostgreSQL at `localhost:9092`, `localhost:27017`, and `localhost:5432`. Override a dependency when
needed:

```powershell
$env:KafkaOptions__BootstrapServers="localhost:9092"
```

Dockerized callers reach the host-run service through
`http://host.docker.internal:<service-port>/`. Portal local routing lives in
`src/Defender.Portal/src/WebUI/appsettings.Local.json`.

For Dev behavior, replace `local` with `dev`, run with `ASPNETCORE_ENVIRONMENT=Dev`, and apply the
same `host.docker.internal` routing rule. Kafka prefixes are `local_` for `Local`, `DockerLocal`, and
`Debug`; `dev_` for `Dev` and `DockerDev`.

Verification and common fixes:

- Read caller logs with `docker compose -f src/docker-compose.yml --profile local logs --tail=200 <caller-service-name>`.
- `Address already in use`: stop the matching compose service.
- One-way connectivity: point the containerized caller at `host.docker.internal`.
- Kafka timeout: verify bootstrap servers, broker health, and topic leader.
- `401` or invalid JWT: ensure all processes use the same `Defender_App_JwtSecret`.

Personal Food Advisor example:

```powershell
docker compose -f src/docker-compose.yml --profile local up -d
docker compose -f src/docker-compose.yml --profile local stop local-personal-food-advisor-service
# Run WebApi Debug profile at http://localhost:47062
```

## Create a service from template

Choose:

- Solution name: `Defender.{ServiceName}`.
- Kebab name: `{kebab-name}` matching `scripts/map-service-name.sh`.
- Next free Local and Dev ports from `src/service-template/README.md`.

Scaffold and rename:

1. Copy `src/service-template` to `src/Defender.{ServiceName}`.
2. Rename `Defender.ServiceTemplate.sln` to `Defender.{ServiceName}.sln`.
3. Replace `Defender.ServiceTemplate`, display-name variants, and `Defender_ServiceTemplate` only
   inside the copied directory.
4. Set `src/WebApi/Properties/launchSettings.json` port and Swagger title in
   `src/WebApi/ConfigureServices.cs`.
5. Rewrite the copied README and update the template port table.
6. Restore the new solution and fix stale project references.

Integrate the service everywhere:

- Add its projects to `src/Defender.Core.slnx` following an existing WebApi service.
- Add `helm/service-template/values-{kebab-name}.yaml` with repository, pinned image tag, and host.
- Add workflow input and matrix entry to `.github/workflows/docker-build-publish.yml` using
  `Dockerfile.Service`, `WebApi`, and the new service directory.
- Add the solution name to `scripts/all_systems.sh`.
- Add solution-to-kebab mapping to `scripts/map-service-name.sh`.
- Add generation call to `scripts/generate-argocd-apps.sh`.
- Standard services reuse `src/Dockerfile.Service`; do not duplicate it.

Verify:

```powershell
dotnet restore src/Defender.{ServiceName}/Defender.{ServiceName}.sln
dotnet build src/Defender.{ServiceName}/Defender.{ServiceName}.sln
```

Confirm all workflow, script, Helm, image, and service names match. Do not promote a built image
by default. Before promotion, ArgoCD deployment, or any production smoke test, ask for and receive
explicit user approval in the current task. After approval, promote the pinned tag for every
deployable app changed by the same feature.

## Tests

Run full or focused tests:

```powershell
dotnet test Defender.Core.sln
dotnet test src/Defender.WalletService/Defender.WalletService.sln
dotnet test src/Defender.WalletService/src/Tests/Defender.WalletService.Tests.csproj
```

Keep service tests under `src/Tests/` using folders defined in root `AGENTS.md`. Name tests
`Method_WhenCondition_ExpectedResult`.

## Portal browser smoke testing

Demo environment: `https://portal.coded-by-danil.dev/`

- User: `demo@demo.com`
- Password: `demo2024`

Credentials belong only to demo account. Use them for Playwright portal smoke tests; never reuse
them for privileged or production accounts.
