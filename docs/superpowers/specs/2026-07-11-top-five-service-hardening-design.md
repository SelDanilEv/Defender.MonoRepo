# Top Five Service Hardening Design

## Goal

Resolve five highest-impact audit findings without pushing changes. All commits stay on local `main`.

## Scope and Task Boundaries

### 1. CORS policy correctness and origin restriction

Create one reusable CORS policy registration in `Defender.Common`. Every Web API/Web UI service must register the policy it already invokes. Production allows only HTTPS origins under `coded-by-danil.dev`, including the apex and `home.coded-by-danil.dev`; local and development environments may add explicit localhost origins. The policy allows required methods and headers but does not enable credentials.

Tests prove the policy resolves, rejects an unrelated origin, and does not require a wildcard origin.

### 2. Shared JWT audience validation

Use the agreed `defender-api` audience. Identity user-token issuance and shared internal-token issuance emit this audience. Every service validates issuer, signature, and audience through `JwtTokenAudience` configuration. Tests accept correctly issued tokens and reject missing or different audiences.

### 3. Dependency vulnerability remediation

Remove high and critical dependency findings from the Portal and shared .NET dependency graph. Replace the obsolete Create React App build chain when required by an unfixable advisory path; preserve Portal commands and production static-file output. Upgrade or replace vulnerable .NET packages centrally, then regenerate lockfiles only when package resolution changes.

Tests run Portal lint, tests, production build, `npm audit --omit=dev --audit-level=high`, and .NET vulnerable-package checks. Any residual advisory must be documented with its non-exploitable scope and a tracked remediation, rather than silently ignored.

### 4. Kubernetes workload hardening

Run service containers as an unprivileged user and add chart-level pod/container security settings: no privilege escalation, dropped Linux capabilities, non-root execution, disabled service-account token mount, and read-only root filesystem where application runtime supports it. Use writable ephemeral mounts only for required runtime paths. Preserve probes, resources, and application ports.

Render Helm manifests and inspect generated security contexts. Build both Docker images and start representative services to verify writable-path requirements.

### 5. Deterministic, security-gated delivery

Replace Travel Calendar's mutable image reference with an immutable release reference. Add CI checks for secret scanning, dependency vulnerability thresholds, container-image scanning, and Helm rendering/linting. Checks run for pull requests; publishing and promotion behavior remains unchanged. Fail closed on high or critical findings except explicitly reviewed baseline exceptions.

Tests validate workflow YAML, render every service values file, and verify deployed image references are immutable.

## Delivery Model

Five implementer subagents run sequentially on local `main` to avoid overlapping edits. Each owns one task, writes or updates tests first where code behavior changes, self-reviews, and commits only its files. A task reviewer validates each committed task before next task begins. A final reviewer checks full `main` diff since the merge base. No commit is pushed.

## Non-goals

- Per-service JWT audiences or token-exchange architecture.
- Application-specific dependency readiness probes.
- Broad network-policy rollout without verified ingress and dependency topology.
- Production deployment or remote Git operations.
