# Defender Platform Architecture Review

This document tracks the architecture issues that are still open or only partially addressed in the current working tree as of 2026-03-08. Findings that were already fixed were removed so this file stays actionable.

Severity levels:

- **Critical**: Security vulnerabilities, privilege-escalation paths, or silent data-loss risks that can compromise the platform.
- **High**: Serious architectural and operational weaknesses that materially reduce production readiness.
- **Medium**: Maintainability, consistency, and testability problems that are still worth planning and tracking.
- **Low**: Cleanup and modernization work with limited immediate operational impact.

---

## Table of Contents

- [Critical](#critical)
  - [C1. Authorization Is Still Commented Out on Admin and Secret Endpoints](#c1-authorization-is-still-commented-out-on-admin-and-secret-endpoints)
  - [C2. Any Service Can Mint Cross-Platform Admin Tokens](#c2-any-service-can-mint-cross-platform-admin-tokens)
- [High](#high)
  - [H1. Async-Over-Sync Startup Call Still Exists in IdentityService](#h1-async-over-sync-startup-call-still-exists-in-identityservice)
  - [H2. Kafka Request-Response Is Still Fragile Under Concurrency](#h2-kafka-request-response-is-still-fragile-under-concurrency)
  - [H3. No Centralized Observability](#h3-no-centralized-observability)
  - [H4. JWT Sessions Are Long-Lived and Not Revocable](#h4-jwt-sessions-are-long-lived-and-not-revocable)
  - [H5. Portal Still Exposes JWTs to Browser JavaScript](#h5-portal-still-exposes-jwts-to-browser-javascript)
- [Medium](#medium)
  - [M1. Frontend Redux Migration Is Only Partially Done](#m1-frontend-redux-migration-is-only-partially-done)
  - [M2. Portal Frontend Dependencies Are Materially Outdated](#m2-portal-frontend-dependencies-are-materially-outdated)
  - [M3. Pervasive `any` Types Remain in the Portal](#m3-pervasive-any-types-remain-in-the-portal)
  - [M4. Most Service Solutions Still Exclude Test Projects](#m4-most-service-solutions-still-exclude-test-projects)
  - [M5. There Is Still No Integration or Contract Test Harness](#m5-there-is-still-no-integration-or-contract-test-harness)
  - [M6. BaseMongoRepository Still Uses `.Result` After `Task.WhenAll`](#m6-basemongorepository-still-uses-result-after-taskwhenall)
- [Low](#low)
  - [L1. Frontend Requests Still Cannot Be Cancelled](#l1-frontend-requests-still-cannot-be-cancelled)
  - [L2. Commented-Out Code Remains in BudgetTracker](#l2-commented-out-code-remains-in-budgettracker)
  - [L3. `AddWebUIServices` Naming Is Still Inconsistent](#l3-addwebuiservices-naming-is-still-inconsistent)
  - [L4. Frontend Config Still Contains Hardcoded Values](#l4-frontend-config-still-contains-hardcoded-values)
  - [L5. BaseMongoRepository Constructor Still Uses Redundant Null-Coalescing](#l5-basemongorepository-constructor-still-uses-redundant-null-coalescing)
  - [L6. `LocalSecretsHelper` Remains Duplicated Across Services](#l6-localsecretshelper-remains-duplicated-across-services)

---

## Critical

### C1. Authorization Is Still Commented Out on Admin and Secret Endpoints

**Status**: Open

**Locations**:
- `src/Defender.JobSchedulerService/src/WebApi/Controllers/V1/JobManagementController.cs`
- `src/Defender.GeneralTestingService/src/WebApi/Controllers/V1/TestController.cs`
- `tools/Defender.SecretManagementService/src/WebApi/Controllers/V1/SecretController.cs`

**Current state**: Role-based `[Auth(...)]` attributes are still commented out on endpoints that manage scheduled jobs, generate SuperAdmin JWTs, and read or mutate secrets. The shared `BaseApiController` does not apply a global authorization requirement.

**Why this is a problem**: These endpoints remain reachable without the intended role checks. That is an immediate privilege-escalation path:
- `JobManagementController` can create, start, update, or delete scheduled jobs.
- `TestController` can trigger regression flows and mint a SuperAdmin JWT.
- `SecretController` can read and overwrite encrypted secrets.

**Recommendation**:
1. Restore `[Auth(Roles.Admin)]` and `[Auth(Roles.SuperAdmin)]` immediately.
2. Add automated controller tests that fail if these attributes are removed again.
3. If local development needs a bypass, do it with environment-specific policy wiring, not commented source code.

---

### C2. Any Service Can Mint Cross-Platform Admin Tokens

**Status**: Open

**Locations**:
- `src/Defender.Common/src/Defender.Common/Helpers/InternalJwtHelper.cs`
- `src/Defender.Common/src/Defender.Common/Accessors/AuthenticationHelper.cs`
- `src/Defender.Common/src/Defender.Common/Consts/Roles.cs`
- `src/Defender.IdentityService/src/WebApi/ConfigureServices.cs`
- `src/Defender.JobSchedulerService/src/WebApi/ConfigureServices.cs`
- `src/Defender.Portal/src/WebUI/ConfigureServices.cs`
- `tools/Defender.SecretManagementService/src/WebApi/ConfigureServices.cs`

**Current state**:
- `InternalJwtHelper.GenerateInternalJWTAsync` issues internal JWTs with every role from `Roles.Any`, including `SuperAdmin`.
- `AuthenticationHeaderAccessor` uses those tokens for `AuthorizationType.Service`.
- Services validate the same shared symmetric `JwtSecret`.
- JWT validation is configured with `ValidateAudience = false`, so there is no service-specific audience boundary.

**Why this is a problem**: A compromise of any service instance or leak of the shared JWT secret becomes a full-platform compromise. Any service that can generate a service token can call downstream endpoints as an admin or super admin because the platform does not distinguish "service identity" from "human admin identity".

**Recommendation**:
1. Split user authentication from service authentication.
2. Stop minting service tokens with human role claims.
3. Give each service its own audience and narrow service scopes.
4. Prefer asymmetric signing or an identity provider that issues dedicated service principals.

---

## High

### H1. Async-Over-Sync Startup Call Still Exists in IdentityService

**Status**: Partially fixed

**Location**: `src/Defender.IdentityService/src/WebApi/ConfigureServices.cs`

**Current state**: Earlier uses of `Task.WaitAll` and `.Result` were cleaned up elsewhere, but `SecretsHelper.GetSecretAsync(Secret.JwtSecret).Result` still blocks during JWT configuration in IdentityService startup.

**Why this is a problem**: Blocking on async I/O during application startup is still an avoidable deadlock and startup-latency risk. It is also inconsistent with the newer `GetSecretSync` pattern already adopted in other services.

**Recommendation**: Replace the remaining `.Result` call with the synchronous helper already used across the rest of the repo, or move secret resolution into a properly async startup path.

---

### H2. Kafka Request-Response Is Still Fragile Under Concurrency

**Status**: Partially fixed

**Location**: `src/Defender.Kafka/src/Defender.Kafka/CorrelatedMessage/KafkaRequestResponseService.cs`

**Current state**: The tight busy-loop was improved by switching to `Consume(TimeSpan)` with a bounded poll interval, but the design is still brittle:
- a new producer and consumer are created for every request,
- the consumer still shares a response topic and group,
- unmatched messages are ignored rather than routed to the caller that owns them,
- there is no stable per-instance response subscription model.

**Why this is a problem**: Under concurrent load, request-response traffic can still become expensive, hard to reason about, and vulnerable to response races across callers. This is especially risky if more than one in-flight request listens on the same response topic and consumer group.

**Recommendation**:
1. Reuse long-lived producer and consumer instances.
2. Isolate responses by instance, partition, or dedicated reply topic.
3. Move correlation handling out of the raw consume loop into a dispatcher that tracks in-flight requests safely.

---

### H3. No Centralized Observability

**Status**: Open

**Current state**: Services log through Serilog and MediatR behaviors, but there is still no platform-level log aggregation, distributed tracing, or metrics pipeline.

**Why this is a problem**: For a multi-service system using HTTP, Kafka, MongoDB, and background workers, production diagnosis remains much harder than it needs to be. Cross-service failures, queue backlogs, and degraded dependencies are still difficult to correlate quickly.

**Recommendation**:
1. Ship logs to a centralized backend such as Loki or Elasticsearch.
2. Add OpenTelemetry tracing for ASP.NET Core, outgoing HTTP, and Kafka flows.
3. Export metrics for request latency, failure rates, background jobs, and Kafka consumer lag.

---

### H4. JWT Sessions Are Long-Lived and Not Revocable

**Status**: Open

**Locations**:
- `src/Defender.IdentityService/src/Application/Services/TokenManagementService.cs`
- `src/Defender.IdentityService/src/Application/Services/LoginHistoryService.cs`
- `src/Defender.IdentityService/src/Infrastructure/Repositories/LoginRecordRepository.cs`
- `src/Defender.Portal/src/WebUI/Controllers/V1/AuthorizationController.cs`

**Current state**:
- user JWTs are issued for 30 days,
- logout only clears the browser cookie,
- login history is written to MongoDB but never used to revoke or validate sessions.

**Why this is a problem**: Once a token is stolen, the platform has no server-side way to invalidate it before expiry. Logout does not terminate the session from the server's point of view, so a leaked token remains valid for up to 30 days.

**Recommendation**:
1. Shorten access-token lifetime substantially.
2. Introduce refresh tokens with rotation.
3. Add server-side revocation support keyed by `jti`, session id, or login record id.
4. Invalidate active sessions on password reset, manual admin lock, and explicit logout.

---

### H5. Portal Still Exposes JWTs to Browser JavaScript

**Status**: Open

**Locations**:
- `src/Defender.Portal/src/WebUI/Controllers/V1/AuthorizationController.cs`
- `src/Defender.Portal/src/WebUI/ClientApp/src/reducers/sessionReducer.tsx`
- `src/Defender.Portal/src/WebUI/ClientApp/src/services/AuthorizationService.tsx`

**Current state**: The secure cookie improvement is in place, but the login/create endpoints still return the JWT to the SPA, the reducer still stores it in Redux state, and `AuthorizationService` still posts it to `window.opener` during SSO flows.

**Why this is a problem**: The localStorage leak was fixed, but active-session XSS can still read the JWT from application memory. The current approach also keeps two parallel auth channels alive at once: secure cookie auth and bearer-token-in-JavaScript auth.

**Recommendation**:
1. Stop returning the access token to the SPA for normal browser flows.
2. Keep browser auth cookie-based by default.
3. If popup SSO still needs a token handoff, use a one-time exchange code instead of posting the JWT itself across windows.

---

## Medium

### M1. Frontend Redux Migration Is Only Partially Done

**Status**: Partially fixed

**Locations**:
- `src/Defender.Portal/src/WebUI/ClientApp/src/state/store.tsx`
- `src/Defender.Portal/src/WebUI/ClientApp/src/state/hooks.ts`
- many components under `src/Defender.Portal/src/WebUI/ClientApp/src/`

**Current state**: The store now uses `configureStore`, typed hooks were introduced, and some components moved away from legacy access patterns. However, a large portion of the UI still relies on `connect`, `mapStateToProps`, and untyped Redux plumbing.

**Why this is a problem**: The codebase is now split between two Redux styles, which increases onboarding cost and slows future cleanup. It also keeps the remaining components anchored to the same weak typing that caused many of the original frontend issues.

**Recommendation**: Standardize new work on hooks plus typed selectors, then migrate the highest-churn connected components incrementally instead of trying to do the entire UI in one pass.

---

### M2. Portal Frontend Dependencies Are Materially Outdated

**Status**: Open

**Location**: `src/Defender.Portal/src/WebUI/ClientApp/package.json`

**Current state**: The frontend still targets React 17, `react-scripts` 5, and TypeScript 4.7.x.

**Why this is a problem**: This keeps the frontend on an older React and CRA stack while the ecosystem has already moved on. It increases the cost of future upgrades, reduces access to current tooling, and leaves the app depending on packages with shrinking maintenance value.

**Recommendation**:
1. Plan a React 19 upgrade path.
2. Replace or phase out Create React App.
3. Upgrade TypeScript and related typings together as one tracked frontend modernization effort.

---

### M3. Pervasive `any` Types Remain in the Portal

**Status**: Open

**Locations**:
- `src/Defender.Portal/src/WebUI/ClientApp/src/components/`
- `src/Defender.Portal/src/WebUI/ClientApp/src/content/`
- `src/Defender.Portal/src/WebUI/ClientApp/src/layouts/`
- reducer and helper files across the frontend

**Current state**: The recent cleanup reduced some untyped surfaces, but the portal still contains many `props: any`, `state: any`, `dispatch: any`, `useState<any>`, and cast-heavy patterns.

**Why this is a problem**: The portal continues to pay the cost of TypeScript without getting much of the safety benefit. Refactors remain fragile, and Redux/component boundaries are still easy to break silently.

**Recommendation**: Prioritize shared component props, Redux state slices, and the most reused page-level containers first. Tightening those types will remove a large amount of downstream `any` usage.

---

### M4. Most Service Solutions Still Exclude Test Projects

**Status**: Open

**Locations**:
- `src/Defender.BudgetTracker/Defender.BudgetTracker.sln`
- `src/Defender.GeneralTestingService/Defender.GeneralTestingService.sln`
- `src/Defender.IdentityService/Defender.IdentityService.sln`
- `src/Defender.JobSchedulerService/Defender.JobSchedulerService.sln`
- `src/Defender.Kafka/Defender.Kafka.sln`
- `src/Defender.NotificationService/Defender.NotificationService.sln`
- `src/Defender.Portal/Defender.Portal.sln`
- `src/Defender.RiskGamesService/Defender.RiskGamesService.sln`
- `src/Defender.UserManagementService/Defender.UserManagementService.sln`
- `src/Defender.WalletService/Defender.WalletService.sln`
- `src/service-template/Defender.ServiceTemplate.sln`

**Current state**: Some solutions include tests now, but most service-local solution files still do not.

**Why this is a problem**: Opening a service solution in an IDE still often omits the tests that belong to that service, which makes local development and review workflows less reliable than they should be.

**Recommendation**: Add each service's test project to its nearest service solution and make that part of the service template so the drift does not continue.

---

### M5. There Is Still No Integration or Contract Test Harness

**Status**: Open

**Current state**: The repo now has more unit coverage, but there is still no shared harness for API integration tests, consumer-provider contract tests, or service-to-service happy-path verification.

**Why this is a problem**: For a distributed system, unit tests alone do not validate auth propagation, serialized contracts, startup wiring, or message-driven workflows across service boundaries.

**Recommendation**:
1. Add WebApplicationFactory-based API integration tests for the highest-risk services.
2. Add contract coverage for generated clients and message contracts.
3. Add a small number of cross-service smoke tests for core money and auth flows.

---

### M6. BaseMongoRepository Still Uses `.Result` After `Task.WhenAll`

**Status**: Open

**Location**: `src/Defender.Common/src/Defender.Common/DB/Repositories/BaseMongoRepository.cs`

**Current state**: The paginated query path still awaits `Task.WhenAll(totalTask, itemsTask)` and then reads `totalTask.Result` and `itemsTask.Result`.

**Why this is a problem**: This is less severe than a raw blocking wait, but it is still needlessly mixing async and synchronous result access in a shared repository base class.

**Recommendation**: Replace `.Result` with awaited values captured after `Task.WhenAll`, keeping the method consistently async end-to-end.

---

## Low

### L1. Frontend Requests Still Cannot Be Cancelled

**Status**: Open

**Location**: `src/Defender.Portal/src/WebUI/ClientApp/src/api/APIWrapper/APICallWrapper.tsx`

**Current state**: The shared fetch wrapper still does not expose an `AbortSignal` or cancellation pattern to callers.

**Why this is a problem**: Route changes and repeated clicks can still leave obsolete requests running and completing after the user has moved on.

**Recommendation**: Allow callers to pass `signal`, then use `AbortController` in the highest-churn screens first.

---

### L2. Commented-Out Code Remains in BudgetTracker

**Status**: Open

**Location**: `src/Defender.BudgetTracker/src/Infrastructure/ConfigureServices.cs`

**Current state**: Old DI registration code is still commented out next to the active implementation.

**Why this is a problem**: Dead code in infrastructure setup makes review and maintenance slower than necessary.

**Recommendation**: Remove the obsolete registrations and keep only the active client wiring.

---

### L3. `AddWebUIServices` Naming Is Still Inconsistent

**Status**: Open

**Locations**:
- `src/Defender.BudgetTracker/src/WebApi/ConfigureServices.cs`
- `src/Defender.JobSchedulerService/src/WebApi/ConfigureServices.cs`
- `src/Defender.PersonalFoodAdvisor/src/WebApi/ConfigureServices.cs`

**Current state**: Several backend Web API services still register their HTTP layer with `AddWebUIServices(...)`.

**Why this is a problem**: The method name implies frontend UI wiring even though these are backend APIs.

**Recommendation**: Rename these extensions to `AddWebApiServices(...)` for consistency with the rest of the repo.

---

### L4. Frontend Config Still Contains Hardcoded Values

**Status**: Open

**Location**: `src/Defender.Portal/src/WebUI/ClientApp/src/config.json`

**Current state**: Static configuration still contains baked-in values such as the Google client id and app metadata.

**Why this is a problem**: Frontend runtime configuration is still coupled to a checked-in JSON file, which makes environment-specific rollout harder than necessary.

**Recommendation**: Move deploy-time values to an injected runtime config mechanism and keep the checked-in file limited to safe defaults.

---

### L5. BaseMongoRepository Constructor Still Uses Redundant Null-Coalescing

**Status**: Open

**Location**: `src/Defender.Common/src/Defender.Common/DB/Repositories/BaseMongoRepository.cs`

**Current state**: The constructor still initializes instance fields with `_client ??=` and `_database ??=` even though those fields are never set before the constructor runs.

**Why this is a problem**: It suggests lazy or shared initialization that is not actually happening and makes the repository base look more complex than it is.

**Recommendation**: Replace the null-coalescing assignment with normal direct assignment.

---

### L6. `LocalSecretsHelper` Remains Duplicated Across Services

**Status**: Open

**Locations**:
- `src/Defender.BudgetTracker/src/Application/Helpers/LocalSecretHelper/LocalSecretsHelper.cs`
- `src/Defender.GeneralTestingService/src/Application/Helpers/LocalSecretHelper/LocalSecretsHelper.cs`
- `src/Defender.IdentityService/src/Application/Helpers/LocalSecretHelper/LocalSecretsHelper.cs`
- `src/Defender.JobSchedulerService/src/Application/Helpers/LocalSecretHelper/LocalSecretsHelper.cs`
- `src/Defender.NotificationService/src/Application/Helpers/LocalSecretHelper/LocalSecretsHelper.cs`
- `src/Defender.PersonalFoodAdvisor/src/Application/Helpers/LocalSecretHelper/LocalSecretsHelper.cs`
- `src/Defender.Portal/src/Application/Helpers/LocalSecretHelper/LocalSecretsHelper.cs`

**Current state**: The helper is still copied across multiple services with only minor service-specific variation.

**Why this is a problem**: Secret-resolution behavior keeps drifting between services instead of living behind one shared abstraction.

**Recommendation**: Move the shared helper behavior into `Defender.Common` and keep only service-specific enum definitions in each service.
