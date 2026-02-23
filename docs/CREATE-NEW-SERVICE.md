# Create a new Defender service from template

**For use by any AI coding agent (e.g. Cursor, Claude Code). Follow this guide when asked to create a new Defender service from the template.**

---

## Repository overview

Defender is a .NET monorepo: multiple microservices (Clean Architecture: Domain, Application, Infrastructure, WebApi), shared libraries, Helm charts for deployment, and Argo CD for GitOps. Services live under `src/`; each has its own solution folder (e.g. `src/Defender.WalletService/`). The code template is `src/service-template/`; the Helm chart template is `helm/service-template/` with per-service `values-{kebab-name}.yaml`.

**Build and run**: From repo root, build with `dotnet build` from the service directory or use the solution under `src/`. See main [README.md](../README.md) and [src/service-template/README.md](../src/service-template/README.md) for ports and setup.

---

## Creating a new Defender service

Use this section when the user asks to scaffold a new service, add a new service to the monorepo, or create a service from the template. Follow every step; do not skip repository integrations.

### 1. Inputs

Obtain or derive:

- **Solution-style service name**: `Defender.{ServiceName}` (e.g. `Defender.PaymentService`).
- **Kebab name**: `{kebab-name}` for Helm, Argo, and Docker image names (e.g. `payment-service`). Must match the mapping in `scripts/map-service-name.sh`.
- **Ports**: Next free Local and Dev ports from the table in [src/service-template/README.md](../src/service-template/README.md). As of this guide, next free are **Local 47062** and **Dev 49062**; update the template README after assigning.

### 2. Code steps (from template)

1. **Copy the template**: Copy the entire folder `src/service-template` to `src/Defender.{ServiceName}` (e.g. `src/Defender.PaymentService`).
2. **Rename the solution file**: In the new folder, rename `Defender.ServiceTemplate.sln` to `Defender.{ServiceName}.sln`.
3. **Bulk replace in the new folder only** (all `.cs`, `.csproj`, `.json`, and other text files under the new service directory):
  - `Defender.ServiceTemplate` → `Defender.{ServiceName}` (namespaces, `using` directives, `.csproj` RootNamespace and AssemblyName).
  - `Service Template` and `ServiceTemplate` in UI strings and descriptions → human-readable service name (e.g. "Payment Service", "PaymentService" as appropriate).
  - `Defender_ServiceTemplate` in appsettings (e.g. `AppName`) → `Defender_{ServiceName}`.
4. **Set the port**: In `src/Defender.{ServiceName}/src/WebApi/Properties/launchSettings.json`, set `applicationUrl` to the chosen Local port (e.g. `http://localhost:47062`).
5. **Update Swagger title**: In `src/Defender.{ServiceName}/src/WebApi/ConfigureServices.cs`, set the Swagger UI title (e.g. from "Service Template" to the human-readable service name).
6. **Update README**: Edit `src/Defender.{ServiceName}/README.md` for the new service. Optionally add the new service's ports to the ports table in `src/service-template/README.md` for future reference.
7. **Regenerate or fix package references**: After renaming, ensure all `.csproj` ProjectReference and package identities are consistent. Run `dotnet restore` in the new service folder; fix any broken references to the old template names.

### 3. Repository integrations

Apply these changes so the new service is built, deployed, and listed everywhere.

- **Solution**: In [src/Defender.Core.slnx](src/Defender.Core.slnx), add a new `<Folder>` block for `Defender.{ServiceName}` with the same structure as an existing WebApi service (e.g. Defender.WalletService): Application, Domain, Infrastructure, WebApi. Use paths relative to `src/` (e.g. `Defender.PaymentService/src/Application/Application.csproj`). If the service has a Common project, add it like Defender.RiskGamesService or Defender.WalletService.
- **Helm**: In `helm/service-template/`, add a new file `values-{kebab-name}.yaml`. Copy [helm/service-template/values-wallet.yaml](helm/service-template/values-wallet.yaml) and set:
  - `image.repository` (e.g. `defendersd/defender.{kebab-name}` or your registry path).
  - `image.tag` (e.g. initial tag or `latest`).
  - `ingress.hosts[0].host` (e.g. `argo-{kebab-name}`).
- **CI/CD**: In [.github/workflows/docker-build-publish.yml](.github/workflows/docker-build-publish.yml):
  - Add `Defender.{ServiceName}` to `workflow_dispatch.inputs.service.options`.
  - Add a new matrix entry under `strategy.matrix.service` with:
    - `name: "defender.{kebab-name}"`
    - `dockerfile: "Dockerfile.Service"`
    - `project_type: "WebApi"`
    - `service_dir: "Defender.{ServiceName}"`
    (Use `Dockerfile.Portal` and `WebUI` only for the Portal service.)
- **Scripts**:
  - [scripts/all_systems.sh](scripts/all_systems.sh): Add `'Defender.{ServiceName}'` to the `all_systems` array.
  - [scripts/map-service-name.sh](scripts/map-service-name.sh): Add a case `"Defender.{ServiceName}") echo "{kebab-name}"`.
  - [scripts/generate-argocd-apps.sh](scripts/generate-argocd-apps.sh): Add a call `generate_argocd_app "Defender.{ServiceName}" "{kebab-name}" "values-{kebab-name}.yaml"`.
- **Docker**: [src/Dockerfile.Service](src/Dockerfile.Service) is shared and uses `SERVICE_DIR`; no change needed for a standard WebApi service.

### 4. Verification

- Build the new service from the `src` directory (e.g. `dotnet build Defender.{ServiceName}/Defender.{ServiceName}.sln` or build via the main solution).
- Confirm the workflow matrix, `map-service-name.sh`, `all_systems.sh`, and `generate-argocd-apps.sh` all use the same `Defender.{ServiceName}` and `{kebab-name}`.

---

## Conventions

- Follow SOLID and KISS. Avoid redundant comments. Prefer clear names and small, focused modules.


