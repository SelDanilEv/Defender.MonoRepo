# Task 1 CarService shell report

Status: PASS for shell build and test gates. Local readiness remains UNVERIFIED because this host has no Docker/Mongo runtime.

Branch: `codex/my-garage`

Authoritative inputs read first: `.superpowers/sdd/2026-09-13-my-garage/task-1-brief.md`, root `README.md`, root `AGENTS.md`, `src/service-template/AGENTS.md`, `src/service-template/README.md`, and `docs/superpowers/specs/2026-09-13-my-garage-design.md`.

## TDD evidence

RED command:

```powershell
dotnet test src/Defender.CarService/src/Tests/Defender.CarService.Tests.csproj -c Debug --no-restore
```

Result: exit 1. Expected missing-project failure:

```text
MSBUILD : error MSB1009: Project file does not exist.
Switch: src/Defender.CarService/src/Tests/Defender.CarService.Tests.csproj
```

GREEN commands:

```powershell
dotnet restore src/Defender.CarService/Defender.CarService.sln
dotnet build src/Defender.CarService/Defender.CarService.sln -c Debug
dotnet test src/Defender.CarService/Defender.CarService.sln -c Debug
dotnet test src/Defender.CarService/src/Tests/Defender.CarService.Tests.csproj -c Debug --no-restore
```

Results:

- Restore: exit 0, 6 projects restored or up to date.
- Build: exit 0, `Build succeeded`, 0 warnings, 0 errors in the initial GREEN run. A fresh post-commit rebuild surfaced one pre-existing `Defender.Common` RCS1102 warning, with 0 errors.
- Full test: exit 0, test assembly loaded, 0 tests found because shell test project is intentionally empty.
- Direct test: exit 0, same empty-shell result.

## Runtime evidence

Command:

```powershell
$env:Defender_App_MongoDBConnectionString='mongodb://localhost:27017/?replicaSet=rs0'
dotnet run --no-build --project src/Defender.CarService/src/WebApi/Defender.CarService.WebApi.csproj --launch-profile Local
```

Results:

- Local process started and listened on `http://localhost:47065`.
- `GET /health`: HTTP 200, `{"Status":"Healthy"}`.
- `GET /health/ready`: UNVERIFIED. Docker engine is unavailable and no MongoDB endpoint answered the readiness ping.
- Local HTTP run logs the existing HTTPS redirection warning because the Local launch profile has no HTTPS endpoint. No Task 1 source or test warning is present; the fresh build warning is outside CarService.

## Implemented files

- `src/Defender.CarService/Defender.CarService.sln`
- `src/Defender.CarService/README.md`
- `src/Defender.CarService/AGENTS.md`
- Domain, Application, Infrastructure, WebApi, and Tests project files.
- Four assembly markers.
- `WebApi/Program.cs` and `WebApi/ConfigureServices.cs`.
- `appsettings.json`, `appsettings.Local.json`, `appsettings.Debug.json`, `appsettings.Dev.json`, `appsettings.Prod.json`.
- `WebApi/Properties/launchSettings.json` with Local port `47065`.
- Restore lock files for the five new projects.

## Registration and scope review

- Project flow: Domain <- Application <- Infrastructure <- WebApi. Tests reference all four service projects.
- Common registration provides `ICurrentAccountAccessor`, `IMongoClient`, Mongo readiness, secret access, and common MediatR pipelines.
- Shell registers `IMongoDatabase` from `MongoDbOptions.GetDatabaseName()` and preserves the shared Mongo client connection for replica-set transactions.
- Shell registers MediatR, FluentValidation, AutoMapper, `TimeProvider.System`, controllers, JWT, ProblemDetails, authorization, CORS, and health endpoints.
- No custom repository registration. Repository registrations wait for Task 3.
- Local and Debug bind `JwtLocalDevelopmentKey`. Dev and Prod do not contain or bind that setting.
- No `appsettings.Development.json`. No Portal, root solution, Compose, Helm, workflow, graph, or deployment file changed.
- No domain contracts or route handlers added.

## Self-review

- AC1 authoritative brief and all required instructions/spec read: PASS.
- AC2 TDD RED before production files: PASS.
- AC3 Task 1 shell file scope: PASS. Git staged paths are under the CarService shell plus this required report.
- AC4 configuration ruling: PASS. Debug, Dev, Prod files exist; Development file absent.
- AC5 Mongo/common registration ruling: PASS. Database and common platform registration present; custom repositories absent.
- AC6 project references and namespaces: PASS. All assemblies use `Defender.CarService.*`.
- AC7 health and local port: PASS for `/health` and port 47065. Readiness: UNVERIFIED without Mongo.
- `git diff --cached --check`: PASS before final stage refresh.

## Concerns

- Empty test result is expected for Task 1. Domain and application tests belong to later tasks.
- Mongo readiness and transaction runtime are not proven on this host because Docker is unavailable.
- Local HTTP startup retains the service-template HTTPS redirect warning; HTTPS endpoint setup is outside this shell task.
- Fresh rebuild reports pre-existing `Defender.Common/Errors/ErrorCodeHelper.cs` RCS1102; no CarService source warning.

Commit: `Add CarService shell` on the final task HEAD. Resolve the exact SHA with `git rev-parse HEAD`.
