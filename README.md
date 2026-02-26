# Defender.MonoRepo

A microservices monorepo for the Defender ecosystem: portal, identity, user management, wallet, gaming, budgeting, notifications, and supporting platform services.

## Table of Contents

- [Technical Overview](#technical-overview)
  - [Architecture](#architecture)
  - [Technology Stack](#technology-stack)
  - [Project Structure](#project-structure)
  - [Development Setup](#development-setup)
  - [Quick Start](#quick-start)
  - [Local Runtime Reference](#local-runtime-reference)
  - [Testing and Quality](#testing-and-quality)
  - [Troubleshooting and Common Ops](#troubleshooting-and-common-ops)
  - [Deployment](#deployment)
  - [CI/CD Pipeline](#cicd-pipeline)
- [Services Overview](#services-overview)
  - [Production Services](#production-services)
  - [Supporting Services](#supporting-services)
  - [Shared Libraries](#shared-libraries)
- [Business Context](#business-context)
  - [Service Business Meanings](#service-business-meanings)
  - [Integration Flow](#integration-flow)
  - [Use Cases](#use-cases)

---

## Technical Overview

### Architecture

The platform follows a microservices architecture with these principles:

- Clean Architecture / DDD layering in each service.
- CQRS pattern (MediatR) for command/query separation.
- REST APIs with OpenAPI/Swagger.
- Event-driven communication through Kafka.
- BFF pattern in `Defender.Portal`.
- HTTP-based service-to-service calls.
- Eventual consistency where asynchronous processing is required.

#### Architecture Layers

Each service typically follows:

```
WebApi / WebUI   -> API layer (controllers, middleware)
Application      -> Business logic (handlers, services, DTOs)
Domain           -> Domain models and contracts
Infrastructure   -> Repositories, clients, external adapters
```

### Technology Stack

#### Backend Technologies

- .NET 10.0 (`TargetFramework` is centralized in `src/Directory.Build.props`)
- ASP.NET Core
- MediatR 14.0.0
- AutoMapper 16.0.0
- FluentValidation 12.1.1
- MongoDB.Driver 3.6.0
- Npgsql 10.0.1 (PostgreSQL access)
- Confluent.Kafka 2.13.0
- JWT Bearer Authentication
- Serilog 4.3.0
- Swashbuckle.AspNetCore 10.1.0

#### Frontend Technologies

- React 17.0.2
- TypeScript 4.7.3
- Material UI 5.16.7
- Redux Toolkit 2.2.3
- React Router 6.3.0
- i18next

#### Infrastructure and DevOps

- Docker
- Kubernetes
- Helm
- ArgoCD
- GitHub Actions

#### Third-Party Integrations

- Brevo / SendinBlue (email delivery)
- Google OAuth
- Exchange-rate integration used by budgeting workflows

### Project Structure

```
Defender.MonoRepo/
|-- .github/workflows/             # GitHub Actions workflows
|-- docs/                          # Project docs
|-- helm/                          # ArgoCD manifests + Helm chart template
|-- scripts/                       # Automation scripts
|-- secrets/                       # Local/dev secret file templates
|-- src/                           # Services, shared libs, docker files
|   |-- Defender.Portal/
|   |-- Defender.IdentityService/
|   |-- Defender.UserManagementService/
|   |-- Defender.WalletService/
|   |-- Defender.RiskGamesService/
|   |-- Defender.NotificationService/
|   |-- Defender.JobSchedulerService/
|   |-- Defender.BudgetTracker/
|   |-- Defender.PersonalFoodAdviser/
|   |-- Defender.GeneralTestingService/
|   |-- Defender.Common/
|   |-- Defender.Kafka/
|   |-- Defender.DistributedCache/
|   |-- Dockerfile.Service
|   |-- Dockerfile.Portal
|   `-- docker-compose.yml
|-- tools/
`-- Defender.Core.sln
```

#### Service Structure

Typical service layout:

```
Defender.ServiceName/
`-- src/
    |-- Application/
    |-- Domain/
    |-- Infrastructure/
    |-- WebApi/ or WebUI/
    `-- Tests/
```

### Development Setup

#### Prerequisites

- .NET 10 SDK
- Docker Desktop
- Node.js 16+ (Portal frontend)

#### Local Development

1. Clone repository
   ```bash
   git clone <repository-url>
   cd Defender.MonoRepo
   ```

2. Restore and build
   ```bash
   dotnet restore Defender.Core.sln
   dotnet build Defender.Core.sln -c Debug
   ```

3. Start local stack (Docker)
   ```bash
   docker compose -f src/docker-compose.yml --profile local up -d --build
   ```

4. Run services locally (optional, outside compose)
   - Run each service from its `WebApi`/`WebUI` project via `dotnet run`
   - Portal client app:
     ```bash
     cd src/Defender.Portal/src/WebUI/ClientApp
     npm install
     npm start
     ```

5. Default local compose endpoints
   - Portal: `http://localhost:47053`
   - Identity API: `http://localhost:47050`
   - User Management API: `http://localhost:47051`
   - Notification API: `http://localhost:47052`
   - Swagger: `{service-base-url}/swagger`

#### Environment Configuration

Services use `appsettings.json` plus environment overrides, usually:

- `appsettings.Local.json`
- `appsettings.Dev.json`
- `appsettings.Debug.json`
- `appsettings.Prod.json`

### Quick Start

Minimal end-to-end bootstrap for a new local environment:

1. Ensure Docker network exists (compose expects `external-network`):
   ```bash
   docker network create external-network
   ```
   Ignore the error if the network already exists.

2. Restore + build once:
   ```bash
   dotnet restore Defender.Core.sln
   dotnet build Defender.Core.sln -c Debug
   ```

3. Start local infra + services:
   ```bash
   docker compose -f src/docker-compose.yml --profile local up -d --build
   ```

4. Verify containers are up:
   ```bash
   docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
   ```

5. Open Portal and Swagger:
   - Portal: `http://localhost:47053`
   - Swagger for any API service: `http://localhost:<port>/swagger`

### Local Runtime Reference

#### Application Services (`--profile local`)

| Service | Container | Local URL | Swagger |
|---|---|---|---|
| Defender.IdentityService | `LocalIdentityService` | `http://localhost:47050` | `http://localhost:47050/swagger` |
| Defender.UserManagementService | `LocalUserManagementService` | `http://localhost:47051` | `http://localhost:47051/swagger` |
| Defender.NotificationService | `LocalNotificationService` | `http://localhost:47052` | `http://localhost:47052/swagger` |
| Defender.Portal | `LocalPortal` | `http://localhost:47053` | N/A |
| Defender.JobSchedulerService | `LocalJobSchedulerService` | `http://localhost:47057` | `http://localhost:47057/swagger` |
| Defender.WalletService | `LocalWalletService` | `http://localhost:47058` | `http://localhost:47058/swagger` |
| Defender.GeneralTestingService | `LocalGeneralTestingService` | `http://localhost:47059` | `http://localhost:47059/swagger` |
| Defender.RiskGamesService | `LocalRiskGamesService` | `http://localhost:47060` | `http://localhost:47060/swagger` |
| Defender.BudgetTracker | `LocalBudgetTrackerService` | `http://localhost:47061` | `http://localhost:47061/swagger` |
| Defender.PersonalFoodAdviser | `LocalPersonalFoodAdviserService` | `http://localhost:47062` | `http://localhost:47062/swagger` |

#### Local Infrastructure Services

| Component | Container | Access |
|---|---|---|
| MongoDB (replica set `rs0`) | `mongo_local` | `localhost:27017` |
| PostgreSQL | `local-postgres-service` | `localhost:5432` |
| ZooKeeper | `local-zookeeper` | `localhost:2181` |
| Kafka broker | `local-kafka-service` | `localhost:9092` |
| Kafka UI | `local-kafka-ui` | `http://localhost:8080` |
| pgAdmin | `local-pgadmin` | `http://localhost:5050` (`admin@example.com` / `admin`) |

### Testing and Quality

#### Run Tests

Run the full monorepo solution tests:

```bash
dotnet test Defender.Core.sln
```

Run tests for a specific service:

```bash
dotnet test src/Defender.WalletService/Defender.WalletService.sln
```

Run a single test project directly:

```bash
dotnet test src/Defender.WalletService/src/Tests/Defender.WalletService.Tests.csproj
```

#### Coverage Dashboard

The repository includes a coverage dashboard generator:

```bash
pwsh scripts/coverage-dashboard.ps1
```

Open the generated report directly:

```bash
pwsh scripts/coverage-dashboard.ps1 -OpenReport
```

If coverage tools are missing, install:

```bash
dotnet tool install --global dotnet-coverage
dotnet tool install --global dotnet-reportgenerator-globaltool
```

Coverage artifacts are written to `artifacts/coverage/dashboard`.

#### Test Layout Convention

Within each `Defender.<Service>` use `src/Tests/` and group tests by layer:

- `Services/`
- `Handlers/`
- `Validators/`
- `Models/`
- `Domain/`
- `Infrastructure/Clients/`
- `Infrastructure/Mappings/`
- `Controllers/`

Use test naming style: `Method_WhenCondition_ExpectedResult`.

### Troubleshooting and Common Ops

#### Common Local Issues

1. `network external-network declared as external, but could not be found`
   - Fix: `docker network create external-network`

2. Port collision on `4705x`, `4706x`, `5432`, `9092`, or `27017`
   - Fix: stop local processes or adjust host port mappings in `src/docker-compose.yml`

3. Service container is running but API is not reachable
   - Check logs: `docker logs <container-name> --tail 200`
   - Confirm environment file exists: `secrets/secrets.local.list`

4. Need clean rebuild of local stack
   - Recreate containers: `docker compose -f src/docker-compose.yml --profile local down`
   - Start again: `docker compose -f src/docker-compose.yml --profile local up -d --build`

#### Helpful Maintenance Commands

```bash
# Stop stack
docker compose -f src/docker-compose.yml --profile local down

# Start dev profile instead of local
docker compose -f src/docker-compose.yml --profile dev up -d --build

# Show only Defender containers
docker ps --filter "name=Local" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

### Deployment

#### Containerization

- `src/Dockerfile.Service` for API services
- `src/Dockerfile.Portal` for Portal

#### Kubernetes Deployment

- Helm chart template: `helm/service-template/`
- Service values files: `helm/service-template/values-*.yaml`
- ArgoCD app definitions: `helm/argocd-applications/`

#### ArgoCD Integration

- Argo project and RBAC configuration: `helm/argocd-config/`
- Image-tag promotion workflow: `.github/workflows/promote-image-tag.yml` (manual dispatch)

See [README-ARGOCD.md](./docs/README-ARGOCD.md) for details.

#### Deployment Environments

- Development workloads: `defender` namespace
- Staging workloads: `defender-staging` namespace
- ArgoCD control plane: `argocd` namespace

### CI/CD Pipeline

#### GitHub Actions Workflows

1. Build and publish (`.github/workflows/docker-build-publish.yml`)
   - Triggers on push to `main`/`develop`, tags, PRs, and manual dispatch
   - Runs test jobs and docker build jobs in parallel
   - Builds per matrix service set (or single selected service)
   - Publishes images to Docker Hub (not on PRs)
   - Includes NuGet egress preflight before test execution

2. Promote image tag (`.github/workflows/promote-image-tag.yml`)
   - Manual dispatch only
   - Updates Helm values image tags
   - Commits and pushes changes when required

#### Build Process

1. Source-change check gates whether matrix jobs run.
2. Tests and image builds execute in parallel jobs.
3. Image tags include branch/SHA/date-based variants.
4. Promotion to ArgoCD values is handled by a separate workflow.

---

## Services Overview

### Production Services

1. Defender.Portal - Main UI and BFF
2. Defender.IdentityService - Authentication and authorization
3. Defender.UserManagementService - User profile management
4. Defender.WalletService - Wallets and transactions
5. Defender.RiskGamesService - Lottery and gaming
6. Defender.NotificationService - Outbound notifications
7. Defender.BudgetTracker - Budget tracking and reviews
8. Defender.PersonalFoodAdviser - Menu parsing and food recommendations

### Supporting Services

1. Defender.JobSchedulerService - Scheduled/recurring background jobs
2. Defender.GeneralTestingService - Platform testing utilities

### Shared Libraries

1. Defender.Common - Shared helpers, errors, base abstractions
2. Defender.Kafka - Kafka integration abstractions
3. Defender.DistributedCache - Distributed cache support

---

## Business Context

### Service Business Meanings

#### Defender.Portal

- Business purpose: the primary user-facing channel and BFF that composes multiple backend services into one coherent product experience.
- Primary responsibilities: authenticate users, orchestrate cross-service workflows, and expose account, wallet, gaming, budget, and food-adviser views in a single UI.
- Key integrations: `Defender.IdentityService`, `Defender.UserManagementService`, `Defender.WalletService`, `Defender.RiskGamesService`, `Defender.BudgetTracker`, `Defender.PersonalFoodAdviser`, and `Defender.NotificationService`.
- Business value: reduces frontend complexity, centralizes user journeys, and shortens time-to-deliver new cross-domain features.

#### Defender.IdentityService

- Business purpose: platform security boundary for authentication, authorization, and access-token lifecycle management.
- Primary responsibilities: credential and OAuth login flows, JWT issuance/validation, access-code flows (verification/reset), and role-based access enforcement.
- Key integrations: Google OAuth provider, `Defender.NotificationService` for verification/recovery messaging, and downstream APIs that validate issued tokens.
- Business value: enforces consistent security policy across all services and minimizes duplicated auth logic in product domains.

#### Defender.UserManagementService

- Business purpose: source of truth for user profile data and account-level metadata outside of credentials.
- Primary responsibilities: create/update profile records, expose user details for internal consumers, and manage profile lifecycle state used by dependent domains.
- Key integrations: `Defender.Portal` (profile UX), `Defender.IdentityService` (identity linkage), and business services that require normalized user information.
- Business value: keeps user data consistent across domains and prevents fragmentation of customer information.

#### Defender.WalletService

- Business purpose: transactional finance core that manages wallets, balances, and money movement.
- Primary responsibilities: wallet/account provisioning, deposits/withdrawals/transfers, transaction history, and reliable processing in synchronous and asynchronous flows.
- Key integrations: `Defender.RiskGamesService` for game payments/payouts, Kafka events for downstream processing, and `Defender.NotificationService` for transaction messaging.
- Business value: provides financial integrity and auditable transaction behavior required for trust and regulatory readiness.

#### Defender.RiskGamesService

- Business purpose: gaming domain engine for lotteries/draws and ticket purchase lifecycle.
- Primary responsibilities: manage draw configuration and availability, validate and register ticket purchases, and handle outcome settlement logic.
- Key integrations: `Defender.WalletService` for payment and winnings, `Defender.NotificationService` for user confirmations, and `Defender.Portal` for game interaction.
- Business value: drives user engagement and monetization through controlled, auditable game operations.

#### Defender.NotificationService

- Business purpose: outbound communication hub for transactional and account-related notifications.
- Primary responsibilities: render/send notifications through provider integrations, track delivery status, and support retry/failure handling workflows.
- Key integrations: Brevo/SendinBlue provider, plus upstream triggers from identity, wallet, gaming, and onboarding workflows.
- Business value: ensures users receive critical messages at the right time, improving trust, activation, and supportability.

#### Defender.BudgetTracker

- Business purpose: personal-finance planning domain for organizing budgets and reviewing financial health.
- Primary responsibilities: manage budget positions/groups, capture review snapshots over time, and support financial overview/reporting features.
- Key integrations: `Defender.Portal` for user interaction and wallet-linked data flows where budget context depends on transactional state.
- Business value: helps users build better financial habits and increases long-term product stickiness.

#### Defender.PersonalFoodAdviser

- Business purpose: food-advisory domain that turns menu data into personalized dining recommendations.
- Primary responsibilities: ingest/parse menu content, manage user preference context, and produce recommendation/session outcomes for UI consumption.
- Key integrations: `Defender.Portal` for user journeys, background/event workflows for parsing, and shared platform services for persistence and messaging.
- Business value: expands platform utility beyond finance and gaming, increasing daily-use potential and user retention.

#### Defender.JobSchedulerService

- Business purpose: scheduler/orchestration backbone for recurring and delayed background execution.
- Primary responsibilities: register and manage schedules, trigger jobs at runtime, and emit execution signals/events to target services.
- Key integrations: Kafka/event-driven infrastructure and internal consumers that need periodic processing without tight coupling.
- Business value: centralizes automation concerns, reduces duplicate cron-like implementations, and improves operational reliability.

#### Defender.GeneralTestingService

- Business purpose: non-customer-facing utility service used for platform verification and integration testing scenarios.
- Primary responsibilities: expose controlled test endpoints/workflows, generate deterministic test interactions, and validate cross-service behavior in local/dev environments.
- Key integrations: local infrastructure stack (Kafka, MongoDB, PostgreSQL) and service APIs under test.
- Business value: improves release confidence and troubleshooting speed by giving teams a repeatable system-level validation surface.

### Integration Flow

#### User Registration and Onboarding

1. User registration -> `IdentityService`
2. Email verification -> `NotificationService`
3. Profile creation -> `UserManagementService`
4. Wallet provisioning -> `WalletService`
5. Onboarding UI -> `Portal`

#### Financial Transaction Flow

1. Transaction request -> `Portal`
2. Validation and execution -> `WalletService`
3. Event publishing -> Kafka
4. Notifications -> `NotificationService`
5. Updated state shown in `Portal`

#### Lottery Purchase Flow

1. Draw discovery -> `Portal` + `RiskGamesService`
2. Ticket selection and purchase -> `RiskGamesService`
3. Payment operation -> `WalletService`
4. Confirmation -> `NotificationService`
5. Updated tickets view -> `Portal`

### Use Cases

#### Personal Finance Management

User capabilities:

- Manage wallet balances and transactions
- Organize budget positions and reviews
- Track financial state through portal views

Services involved: Portal, WalletService, BudgetTracker, UserManagementService

#### Gaming and Entertainment

User capabilities:

- Browse draws and buy tickets
- Track purchase history and results
- Receive winnings and notifications

Services involved: Portal, RiskGamesService, WalletService, NotificationService

#### Account Security

User capabilities:

- Authenticate via credentials or Google OAuth
- Verify email and recover account access
- Manage account profile settings

Services involved: Portal, IdentityService, UserManagementService, NotificationService

#### Administrative Operations

Admin capabilities:

- Monitor users and transactions
- Operate gaming and scheduling workflows
- Review service-level operational behavior

Services involved: Portal (admin views), backend services

---

## Additional Resources

- ArgoCD docs: [docs/README-ARGOCD.md](./docs/README-ARGOCD.md)
- Workflow docs: [.github/workflows/README.md](./.github/workflows/README.md)
- Service-specific docs: service-level `README.md` and `AGENTS.md` files
- API docs: `/swagger` endpoint on each API service
- Demo environment: https://portal.coded-by-danil.dev/

---
