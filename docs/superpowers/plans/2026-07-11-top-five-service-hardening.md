# Top Five Service Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the five highest-impact service security, reliability, runtime-hardening, and delivery risks.

**Architecture:** `Defender.Common` becomes the source for the CORS policy and internal JWT audience. Each API keeps its existing authentication setup but requires the same `defender-api` audience. Docker and Helm provide workload-level least privilege, while CI validates dependencies, secrets, containers, and rendered manifests before merge.

**Tech Stack:** .NET 10, ASP.NET Core, xUnit, React, Vite, Docker Alpine, Helm, GitHub Actions.

## Global Constraints

- All commits remain on local `main`; do not push or create a pull request.
- Production CORS permits `https://coded-by-danil.dev` and HTTPS subdomains only; Local/Development explicitly permits localhost origins; no CORS credentials.
- User and internal JWTs use audience `defender-api`; every API rejects missing or mismatched audiences.
- Do not add dependency-specific health checks or change `/health` endpoint semantics.
- Preserve public Portal behavior and API routes.
- Use test-first changes for behavior changes; every task must have focused tests and a task review before the next task starts.

---

### Task 1: Centralize CORS registration and restrict origins

**Files:**
- Create: `src/Defender.Common/src/Defender.Common/Extension/CorsExtensions.cs`
- Modify: `src/Defender.Common/src/Tests/ServiceRegistrationAndModelsTests.cs`
- Modify: every `src/Defender.*/src/WebApi/Program.cs`, `src/Defender.Portal/src/WebUI/Program.cs`, and `src/service-template/src/WebApi/Program.cs`
- Modify: `src/Defender.TravelCalendarService/src/WebApi/ConfigureServices.cs`

**Interfaces:**
- Produces `CorsExtensions.DefenderCorsPolicy` with exact value `DefenderCors`.
- Produces `IServiceCollection AddDefenderCors(this IServiceCollection services, IWebHostEnvironment environment)`.
- Each application invokes `services.AddDefenderCors(builder.Environment)` before `Build()` and `app.UseCors(CorsExtensions.DefenderCorsPolicy)` before authentication.

- [ ] Write tests that resolve `ICorsService`, retrieve `DefenderCors`, allow `https://coded-by-danil.dev` and `https://home.coded-by-danil.dev`, reject `https://attacker.example`, and assert `SupportsCredentials` is false.
- [ ] Run `dotnet test src/Defender.Common/src/Tests/Defender.Common.Tests.csproj --filter FullyQualifiedName~Cors`; verify failure because `AddDefenderCors` and `DefenderCorsPolicy` do not exist.
- [ ] Implement a shared policy with `WithOrigins("https://coded-by-danil.dev", "https://*.coded-by-danil.dev")`, `SetIsOriginAllowedToAllowWildcardSubdomains()`, `AllowAnyHeader()`, `AllowAnyMethod()`, and no `AllowCredentials()` call. Add explicit localhost origins only when `environment.IsLocalOrDevelopment()`.
- [ ] Replace all `UseCors("AllowAll")` calls with the shared policy; remove Travel Calendar's inline `AllowAll` registration.
- [ ] Re-run focused common tests and build every changed WebApi/WebUI project. Expected: policy test passes and no application references `AllowAll`.
- [ ] Commit with `git add src/Defender.Common src/Defender.* src/service-template && git commit -m "Fix service CORS policies"`.

### Task 2: Require the shared JWT audience

**Files:**
- Modify: `src/Defender.IdentityService/src/Application/Services/TokenManagementService.cs`
- Modify: `src/Defender.Common/src/Defender.Common/Helpers/InternalJwtHelper.cs`
- Modify: every service `ConfigureServices.cs` containing `ValidateAudience = false`
- Modify: affected `appsettings.json` and Local/Debug configuration files that define `JwtTokenIssuer`
- Modify: `src/Defender.IdentityService/src/Tests/**` and `src/Defender.Common/src/Tests/SecretsAndCryptoTests.cs`

**Interfaces:**
- Configuration key: `JwtTokenAudience`, exact value `defender-api`.
- `TokenManagementService` and `InternalJwtHelper.GenerateInternalJWTAsync` create tokens with audience `defender-api`.
- Every `TokenValidationParameters` sets `ValidateAudience = true` and `ValidAudience = configuration["JwtTokenAudience"]`.

- [ ] Add tests creating one valid token, one missing-audience token, and one `wrong-audience` token; validation accepts only the valid token.
- [ ] Run affected tests before implementation; verify the missing and wrong audience cases are currently accepted or configuration does not support the test.
- [ ] Add `JwtTokenAudience: defender-api` to configuration, pass it as the `audience` argument to both JWT constructors, and require it in every API validator.
- [ ] Preserve issuer and signature validation; do not weaken expiration validation or authorization policies.
- [ ] Run Identity, Common, Portal, and one representative API test project, then `dotnet test src/Defender.Core.sln --no-restore --nologo`.
- [ ] Commit with `git add src && git commit -m "Require JWT audience validation"`.

### Task 3: Remove high and critical dependency findings

**Files:**
- Modify: `src/Defender.Portal/src/WebUI/ClientApp/package.json`, `package-lock.json`, and build/test configuration required to retire `react-scripts`
- Create or modify: Portal Vite configuration and root HTML entrypoint as required by migration
- Modify: `src/Directory.Packages.props` and package lockfiles affected by safe .NET upgrades
- Modify: Portal and shared-library tests for build or package changes

**Interfaces:**
- Portal commands remain `npm start`, `npm run build`, `npm test`, and `npm run lint`.
- Production build remains consumable by Portal WebUI static-file hosting.
- `npm audit --package-lock-only --omit=dev --audit-level=high` exits zero.
- `dotnet list src/Defender.Core.sln package --vulnerable --include-transitive --no-restore` has no high or critical findings.

- [ ] Capture baseline audit output and add a focused regression test or build check that demonstrates current CRA chain cannot meet high-severity audit threshold.
- [ ] Replace `react-scripts` with maintained Vite tooling; preserve proxy behavior, TypeScript JSX transforms, SPA entrypoint, production `build` output, and existing test/lint commands.
- [ ] Upgrade or replace vulnerable .NET package versions in central package management; regenerate only changed lockfiles.
- [ ] Run Portal lint, tests, production build, npm audit threshold command, .NET vulnerable-package command, and full .NET tests.
- [ ] Commit with `git add src && git commit -m "Remediate vulnerable service dependencies"`.

### Task 4: Run workloads with least privilege

**Files:**
- Modify: `src/Dockerfile.Service`
- Modify: `src/Dockerfile.Portal`
- Modify: `helm/service-template/values.yaml`
- Modify: `helm/service-template/templates/deployment.yaml`
- Create or modify: Helm rendering tests/scripts if needed to assert security fields

**Interfaces:**
- Final images execute as UID/GID `10001`, not root.
- Values define `podSecurityContext` and `containerSecurityContext`.
- Rendered deployments set `runAsNonRoot: true`, `allowPrivilegeEscalation: false`, `readOnlyRootFilesystem: true`, `capabilities.drop: ["ALL"]`, `seccompProfile.type: RuntimeDefault`, and `automountServiceAccountToken: false`.
- Writable `emptyDir` mounts exist only for runtime paths proven necessary by representative containers.

- [ ] Add Helm assertions or render checks that fail when any required security field is absent.
- [ ] Add a non-root user/group in each final Docker image, copy published files with that ownership, and set `USER 10001:10001` before entrypoint.
- [ ] Add chart security contexts, `/tmp` writable `emptyDir`, and required container mount only if runtime verification proves it necessary.
- [ ] Build both Dockerfiles and run representative Service and Portal containers as rendered; verify `/health` works and process user is not root.
- [ ] Render every `values-*.yaml` file with `helm template`; inspect security settings and probe paths.
- [ ] Commit with `git add src/Dockerfile.* helm/service-template && git commit -m "Harden service workload security"`.

### Task 5: Make delivery immutable and security-gated

**Files:**
- Modify: `helm/service-template/templates/deployment.yaml`
- Modify: `helm/service-template/values-travel-calendar.yaml`
- Modify: `.github/workflows/docker-build-publish.yml`
- Modify: `.github/workflows/README.md`
- Create or modify: validation scripts under `scripts/` only when workflow shell steps cannot remain readable inline

**Interfaces:**
- Image values support an immutable `digest`; deployment renders `repository@digest` when supplied, otherwise keeps existing `repository:tag` behavior.
- Travel Calendar uses digest resolved from its current Docker Hub `latest` manifest at implementation time.
- Pull requests run secret scanning, dependency audits, Trivy image/config scan, `helm lint`, and `helm template` for every service values file.
- Publishing and promotion jobs retain existing triggers and do not push during this task.

- [ ] Write shell-level or workflow validation that fails when Travel Calendar uses `latest` or a deployment with `image.digest` renders a tag instead of `@sha256:`.
- [ ] Query Docker Hub registry manifest for `defendersd/defender.travel-calendar:latest`, record its `sha256` digest in values, and add conditional digest rendering to deployment template.
- [ ] Add pinned security scanner actions and explicit dependency/Helm commands to PR jobs. Configure high/critical failures except for documented baseline entries.
- [ ] Run local Helm lint/template validation and workflow YAML parsing; verify Travel Calendar image is immutable in rendered output.
- [ ] Commit with `git add helm .github scripts && git commit -m "Add secure delivery validation"`.

### Task 6: Whole-system verification and review

**Files:**
- No product source changes expected.

- [ ] Run `dotnet test src/Defender.Core.sln --no-restore --nologo`.
- [ ] Run Portal lint, tests, build, and high-severity npm audit.
- [ ] Run .NET vulnerable-package audit and Helm lint/template for every values file.
- [ ] Run `git diff --check origin/main...HEAD`, inspect commits, and dispatch final reviewer with review package for `origin/main..HEAD`.
- [ ] Resolve every Critical and Important review finding before reporting completion. Do not push.
