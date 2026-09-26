# Plan: Run the whole platform locally with .NET Aspire

## Goal

One command (`aspire run`, or F5 on the AppHost) starts the full Defender platform on a developer machine: all infrastructure containers, all 13 .NET services, the Portal React app, and the MCP server, with a single dashboard for logs, traces and endpoints, and with every service debuggable as a normal .NET process.

Non-goals for this plan: replacing the Helm/ArgoCD deployment path, replacing `src/docker-compose.yml` for the `dev` profile, and producing Aspire deployment manifests for Kubernetes.

## Guiding principle: zero service code changes in the first working version

The platform already reads every downstream URL and every infrastructure endpoint from configuration keys that ASP.NET Core also binds from environment variables. The AppHost therefore injects the existing keys rather than forcing a migration to Aspire service discovery:

| Existing configuration key | Environment variable the AppHost sets |
| --- | --- |
| `IdentityOptions:Url` | `IdentityOptions__Url` |
| `CarServiceOptions:Url` | `CarServiceOptions__Url` |
| `KafkaOptions:BootstrapServers` | `KafkaOptions__BootstrapServers` |
| Mongo connection string (via `SecretsHelper`) | `Defender_App_MongoDBConnectionString` |
| Distributed cache connection string (via `SecretsHelper`) | `Defender_App_DistributedCacheConnectionString` |

This works because `Defender.Common` resolves secrets from environment variables before the Mongo-backed secret store (`SecretsHelper.GetSecretFromEnvVariables` checks Process, then User, then Machine targets), and because `CommonServiceExtensions.AddCommonOptions` overwrites `MongoDbOptions.ConnectionString` from `SecretsHelper` in a `PostConfigure` hook.

Rewriting the 13 services onto `WithReference` plus `services__<name>__http__0` discovery is deferred to a later, optional phase. It is not required to get the platform running.

## Current facts the plan is built on

- 13 deployable .NET services plus the Portal BFF, all on `net10.0` via `src/Directory.Build.props`. No `global.json`; the machine has SDK 10.0.301 and 10.0.201.
- No Aspire anywhere: no `*.AppHost` project, no Aspire package in `src/Directory.Packages.props`, and `dotnet new list aspire` finds no templates.
- Infrastructure today: MongoDB 7.0 as a single-member replica set `rs0`, Kafka (`wurstmeister/kafka`) with Zookeeper, PostgreSQL 16 (`cache_database`), plus optional Prometheus/Loki/Promtail/Grafana.
- The Mongo replica set is initialised **inside the compose healthcheck**, and the member host is `host.docker.internal:27017`. There is no separate init script.
- Host ports are fixed and documented: 47050-47065 for services, 47054 for the Vite dev server, 8080 for Kafka UI, 5050 for pgAdmin.
- Secrets live in the git-ignored `secrets/secrets.local.list` as `KEY=VALUE` lines. The committed templates under `tools/Defender.SecretManagementService/secrets/*.template.list` are stale relative to it.
- `src/Defender.Core.sln` is missing `Defender.GeneralTestingService`, `Defender.HealthCareService` and `src/service-template`.
- CI (`.github/workflows/docker-build-publish.yml`) discovers test projects per `service_dir` matrix entry and never builds the solution as a whole, so a new project under `src/` is not picked up automatically.
- `RestorePackagesWithLockFile` is `true` and `ManagePackageVersionsCentrally` is `true`, so every new project needs a committed lock file and centrally pinned package versions.

## Phase 0 - Prerequisites

1. Add `global.json` at the repository root pinning the SDK feature band, so Aspire tooling and CI resolve the same SDK:

   ```json
   { "sdk": { "version": "10.0.301", "rollForward": "latestFeature" } }
   ```

2. Install the Aspire CLI and project templates. Aspire no longer ships as a `dotnet workload`; it is an SDK reference plus NuGet packages:

   ```bash
   dotnet tool install -g aspire.cli
   ```

   ```bash
   dotnet new install Aspire.ProjectTemplates
   ```

3. Resolve the current stable Aspire version at install time instead of hardcoding one:

   ```bash
   dotnet package search Aspire.Hosting.AppHost --take 1
   ```

4. Confirm Docker Desktop is running and the `external-network` bridge still exists for anyone who keeps using compose in parallel.

**Acceptance:** `aspire --version` prints a version, `dotnet new list aspire` lists the AppHost template, `dotnet --version` reports the pinned SDK from the repository root.

## Phase 1 - AppHost skeleton and infrastructure

Create two projects, matching the existing folder conventions:

```
src/Defender.AppHost/Defender.AppHost.csproj
src/Defender.ServiceDefaults/Defender.ServiceDefaults.csproj
```

`Defender.AppHost.csproj` needs `<Sdk Name="Aspire.AppHost.Sdk" Version="..." />`, `<IsAspireHost>true</IsAspireHost>`, and `<ManagePackageVersionsCentrally>` inherited from `src/Directory.Build.props`. Add the Aspire package versions to `src/Directory.Packages.props`:

- `Aspire.Hosting.AppHost`
- `Aspire.Hosting.MongoDB`
- `Aspire.Hosting.Kafka`
- `Aspire.Hosting.PostgreSQL`
- `Aspire.Hosting.NodeJs`

Then model the infrastructure.

### MongoDB with a working replica set

This is the single hardest part, because every service resolves its secrets from Mongo and the replica set must be initialised before any service starts.

```csharp
var mongo = builder.AddMongoDB("mongo")
    .WithImageTag("7.0")
    .WithArgs("--replSet", "rs0", "--bind_ip_all", "--port", "27017")
    .WithEndpoint("tcp", e => { e.Port = 27017; e.TargetPort = 27017; })
    .WithDataVolume("defender-mongo-data");

var mongoInit = builder.AddContainer("mongo-init", "mongo", "7.0")
    .WithEntrypoint("bash")
    .WithArgs("-c",
        "until mongosh --host host.docker.internal --port 27017 --quiet --eval 'db.adminCommand(\"ping\")'; do sleep 1; done; " +
        "mongosh --host host.docker.internal --port 27017 --quiet --eval " +
        "'try { rs.status() } catch (e) { rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"host.docker.internal:27017\"}]}) }'")
    .WaitFor(mongo);
```

Rules that must not be broken here:

- The host port stays pinned to **27017**. Aspire assigns random host ports by default; a random port breaks the replica-set member address and every developer's `mongosh` habit.
- The replica-set member host stays `host.docker.internal:27017`, exactly as compose has it. The MongoDB driver discovers topology and then connects to the **advertised** member address, so services running as host processes must be able to resolve that name. On Windows with Docker Desktop this works; add a note for anyone on plain Linux Docker that they need `--add-host=host.docker.internal:host-gateway` or a `localhost` member address instead.
- Every service resource must `.WaitForCompletion(mongoInit)`, not merely `.WaitFor(mongo)`. Waiting on the container alone starts services against an uninitialised replica set, which fails transactions in a way that looks like a random data bug.

### Kafka and PostgreSQL

```csharp
var kafka = builder.AddKafka("kafka").WithKafkaUI();

var postgres = builder.AddPostgres("postgres")
    .WithImageTag("16")
    .WithPgAdmin()
    .WithDataVolume("defender-postgres-data");

var cacheDb = postgres.AddDatabase("cache_database");
```

Aspire's Kafka resource uses a KRaft-based image and needs no Zookeeper, so the `local-zookeeper` container disappears in the Aspire path. Advertised listeners are handled by the Aspire integration; services get the bootstrap address from `kafka.Resource.ConnectionStringExpression`.

**Acceptance for Phase 1:**

- `aspire run` starts the AppHost and the dashboard opens.
- `mongo-init` reaches the `Finished` state in the dashboard.
- `mongosh --host host.docker.internal --port 27017 --eval "rs.status().ok"` returns `1`.
- Kafka UI and pgAdmin open from the dashboard resource links.
- Stopping and restarting `aspire run` does not re-initialise or corrupt the replica set (data volumes persist).

## Phase 2 - The 13 services, without touching service code

### Load the existing secrets file

`secrets/secrets.local.list` stays the single source of truth. The AppHost parses it rather than duplicating values into user-secrets:

```csharp
static IReadOnlyDictionary<string, string> LoadSecretsList(string path) =>
    File.Exists(path)
        ? File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#') && line.Contains('='))
            .Select(line => line.Split('=', 2))
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim())
        : throw new InvalidOperationException(
            $"Missing {path}. Copy tools/Defender.SecretManagementService/secrets/secrets.local.template.list and fill it in.");
```

Then fail fast on the keys the platform genuinely cannot fake. This matters because `SecretsHelper` returns an empty string for a missing secret and never throws, so today a missing `Defender_App_SecretsEncryptionKey` surfaces much later as an unexplained decryption failure. The AppHost must turn that into a startup error naming the missing key.

Refresh the committed templates while doing this: they list `Defender_App_JwtSecret` and `Defender_App_HuggingFaceApiKey` but omit `Defender_App_GeminiApiKey` and the `Telegram__*` keys that the real local file uses.

### Register each service as a project resource

```csharp
var identity = builder.AddProject<Projects.Defender_IdentityService_WebApi>("identity")
    .WithHttpEndpoint(port: 47050, name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Local")
    .WithSecretsList(secrets)
    .WithEnvironment("Defender_App_MongoDBConnectionString", mongoConnectionString)
    .WaitForCompletion(mongoInit);
```

Then wire the call graph with endpoint references, which resolve to the real assigned URL:

```csharp
identity
    .WithEnvironment("UserManagementOptions__Url", userManagement.GetEndpoint("http"))
    .WithEnvironment("NotificationOptions__Url", notification.GetEndpoint("http"));

portal
    .WithEnvironment("IdentityOptions__Url", identity.GetEndpoint("http"))
    .WithEnvironment("UserManagementOptions__Url", userManagement.GetEndpoint("http"))
    .WithEnvironment("WalletOptions__Url", wallet.GetEndpoint("http"))
    .WithEnvironment("RiskGamesOptions__Url", riskGames.GetEndpoint("http"))
    .WithEnvironment("BudgetTrackerOptions__Url", budgetTracker.GetEndpoint("http"))
    .WithEnvironment("PersonalFoodAdvisorOptions__Url", personalFoodAdvisor.GetEndpoint("http"))
    .WithEnvironment("HealthCareOptions__Url", healthCare.GetEndpoint("http"))
    .WithEnvironment("TravelCalendarOptions__Url", travelCalendar.GetEndpoint("http"))
    .WithEnvironment("CarServiceOptions__Url", carService.GetEndpoint("http"));
```

The full call graph to encode, taken from the `Configure<...Options>` bindings:

- Portal to Identity, UserManagement, Wallet, RiskGames, BudgetTracker, PersonalFoodAdvisor, HealthCare, TravelCalendar, CarService
- Identity to UserManagement and Notification
- UserManagement to Identity
- RiskGames to Wallet
- GeneralTestingService to Portal (`PortalApiOptions`)
- CarService, HealthCare, TravelCalendar, BudgetTracker, JobScheduler, Notification: no outbound service URLs

Kafka consumers (JobScheduler, Wallet, PersonalFoodAdvisor) additionally get `KafkaOptions__BootstrapServers` from the Kafka resource and `.WaitFor(kafka)`. Wallet also gets `Defender_App_DistributedCacheConnectionString` from `cacheDb`.

### Keep the port numbers

Pin every service to its documented port (47050-47065). The README, the Portal's `appsettings.Local.json` fallbacks, the Playwright e2e specs and developer muscle memory all depend on them. Pinned ports also mean a developer can run part of the stack under Aspire and the rest under compose during migration, as long as both are never up at once.

Note the two ports that differ between profiles today: TravelCalendar (47064 local, 49064 dev) and CarService (47065 local, 49065 dev). The AppHost models the Local profile only.

### Two services with no launchSettings.json

`Defender.PersonalFoodAdvisor` and `Defender.HealthCareService` have no `Properties/launchSettings.json`. Aspire does not require one when the AppHost sets the endpoint explicitly, but add them anyway for consistency with the other 11 services and so `dotnet run` alone still works.

**Acceptance for Phase 2:**

- All 14 project resources reach `Running` in the dashboard.
- Each service's Swagger UI answers on its documented port.
- A Portal endpoint that fans out to a downstream service returns a real response (proves both URL injection and JWT validation).
- An authenticated flow through Identity works end to end (proves the Mongo secret bootstrap resolved `JwtSecret` correctly).
- Setting a breakpoint in any service and hitting it through the Portal works, which is the main gain over compose.

## Phase 3 - Portal front end and the MCP server

### React client (Vite)

```csharp
var clientApp = builder.AddNpmApp("portal-clientapp",
        "../Defender.Portal/src/WebUI/ClientApp", "start")
    .WithHttpEndpoint(port: 47054, env: "PORT")
    .WithEnvironment("ASPNETCORE_URLS", portal.GetEndpoint("http"))
    .WithExternalHttpEndpoints()
    .WaitFor(portal);
```

`vite.config.ts` already proxies `/api` to `process.env.ASPNETCORE_URLS?.split(";")[0]`, falling back to a hardcoded `http://localhost:47053`, so passing the Portal endpoint through `ASPNETCORE_URLS` needs no front-end change.

Two things must be cleaned up here:

- `ClientApp/.env.development.local` hardcodes `SSL_CRT_FILE` and `SSL_KEY_FILE` under `C:\Users\Defender\...`. Those paths do not exist on any other machine. Either remove that file from source control and document `dotnet dev-certs https --export-path`, or drive the paths from an AppHost-provided environment variable.
- `.env` sets `HTTPS=true`. For the Aspire path run the dev server over HTTP and let Aspire own TLS termination, otherwise the proxy target and the certificate have to agree on every machine.

### MCP server (Node)

`src/Defender.Portal.Mcp/src/config.ts` rejects any non-`https:` value for `PORTAL_BASE_URL`, `PORTAL_OAUTH_ISSUER` and `MCP_PUBLIC_URL` via `requireHttpsOrigin`/`requireHttpsIssuer`. Two ways forward:

1. **No code change.** Give the Portal an HTTPS endpoint in the AppHost (Aspire uses the ASP.NET Core dev certificate) and pass `PORTAL_BASE_URL=https://localhost:<port>`. This still leaves `MCP_PUBLIC_URL`, which is the MCP server's own address and would need the Node process to serve HTTPS.
2. **Small, explicit escape hatch (recommended).** Allow `http://localhost` and `http://127.0.0.1` origins when an opt-in variable such as `MCP_ALLOW_INSECURE_LOCAL=true` is set, and have only the AppHost set it. Keep the strict check for every other host so production behaviour is unchanged.

Register it as `builder.AddNpmApp("portal-mcp", "../Defender.Portal.Mcp", "start")` with `PORT`, `PORTAL_BASE_URL`, `MCP_PUBLIC_URL`, `MCP_AUDIENCE` and `MCP_ALLOWED_ORIGINS`, waiting on the Portal. Because `start` runs `node dist/index.js`, the AppHost must either run `npm run build` first or the resource should use a `dev` script that compiles on the fly - add one if it does not exist.

**Acceptance for Phase 3:** the Portal UI loads at its Aspire URL, logs in, and renders a page that calls a downstream service; the MCP server answers its health/OAuth metadata endpoint without throwing at startup.

## Phase 4 - ServiceDefaults and telemetry (optional, incremental)

Only after the platform runs. `Defender.ServiceDefaults` adds OpenTelemetry tracing/metrics with the OTLP exporter, standard health check endpoints (`/health`, `/alive`), and HTTP resilience defaults. Each service opts in with one line in `Program.cs` (`builder.AddServiceDefaults()`), so this can be rolled out one service at a time.

Payoff: distributed traces across the whole call graph in the Aspire dashboard, and real readiness gating so `WaitFor` becomes meaningful instead of a start-order hint.

Watch for a conflict with the existing `Defender__Observability__Metrics__Enabled` switch and the Serilog/Prometheus setup already in place. Decide whether the Aspire dashboard replaces the local Grafana/Loki/Prometheus stack for day-to-day work or runs beside it; running both is possible but doubles the exporter configuration.

## Phase 5 - Optional: real Aspire service discovery

Replace each `XxxOptions.Url` injection with `WithReference(service)` plus `Microsoft.Extensions.ServiceDiscovery` in the client code, so URLs come from `services__<name>__http__0` and load balancing/resilience come for free. This touches all 13 services plus `appsettings.Local.json` in each, and it is the step that makes the Local profile files nearly empty. Do it only if the injection approach proves limiting, and do it one service at a time.

## Phase 6 - Documentation, CI and coexistence

1. Update the root `README.md` Quick Start with the Aspire path as the primary local workflow, keeping the compose commands as the fallback and for the `dev` profile.
2. Update the root `AGENTS.md` with the new project layout entries, and note that new services must be registered in the AppHost.
3. Add `Defender.AppHost` and `Defender.ServiceDefaults` to `src/Defender.Core.sln`, and take the opportunity to add the three projects currently missing from it (`Defender.GeneralTestingService`, `Defender.HealthCareService`, `src/service-template`).
4. Commit the `packages.lock.json` files for both new projects, since `RestorePackagesWithLockFile` is enabled.
5. CI needs no matrix entry for the AppHost, because `docker-build-publish.yml` iterates an explicit `service_dir` list and discovers tests per directory. Add a cheap guard job that runs `dotnet build src/Defender.AppHost/Defender.AppHost.csproj -c Release` so a broken AppHost is caught before someone tries to run it.
6. Add a note that the Aspire stack and `docker compose --profile local` must not run at the same time, because both bind 27017, 5432, 9092 and 47050-47065.

## Risk register

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Mongo replica set not initialised before services start | Transactions fail intermittently, looks like a data bug | One-shot `mongo-init` container plus `WaitForCompletion` on every service |
| Aspire assigns a random host port to Mongo | Replica-set member address stops matching, driver cannot connect | Pin host port 27017 explicitly |
| `host.docker.internal` unresolvable on non-Docker-Desktop hosts | Nothing connects to Mongo | Document the `host-gateway` alias; consider a `localhost` member address variant |
| Missing secret returns empty string silently | Failures appear far from the cause | AppHost validates required keys and fails fast with the key name |
| Stale secret templates | New developers fill in the wrong key set | Regenerate templates from the real key list in Phase 2 |
| MCP HTTPS-only validation | MCP crashes at startup under a local HTTP Portal | Opt-in `MCP_ALLOW_INSECURE_LOCAL` limited to loopback hosts |
| Machine-specific certificate paths in `ClientApp/.env.development.local` | Front end fails to start for everyone else | Remove from source control, drive from environment |
| Aspire stack and compose running together | Port collisions across 27017/5432/9092/47050-47065 | Documented warning plus a preflight port check in the AppHost |
| Central package management friction with the AppHost SDK | Restore errors on the new project | Pin all Aspire versions in `src/Directory.Packages.props`, commit lock files |

## Effort estimate

| Phase | Scope | Rough effort |
| --- | --- | --- |
| 0 | Tooling, `global.json` | under an hour |
| 1 | AppHost skeleton, Mongo/Kafka/Postgres, replica-set init | half a day to a day, most of it on the Mongo bootstrap |
| 2 | 14 project resources, secrets loading, call-graph wiring | one to two days |
| 3 | Vite client and Node MCP server | half a day, plus the MCP config change |
| 4 | ServiceDefaults rollout | half a day per batch of services, incremental |
| 5 | True service discovery | optional, one to two days |
| 6 | Docs, solution, CI guard | half a day |

The platform is runnable end to end at the end of Phase 3.
