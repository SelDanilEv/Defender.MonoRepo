# Defender Observability New App Proposal

## Goal

Introduce centralized observability for the existing Defender microservices with minimal risk to running services.

Primary outcomes:
- One place to see service health, latency, error rates, and pod/resource state.
- Fast incident triage without jumping across pods.
- Incremental rollout that does not force a large refactor of every service.

---

## Current Solution Constraints (from this repo)

- All services are deployed by ArgoCD using `helm/service-template` and per-service values files.
- Service template currently has no probe definitions and no metrics scrape annotations.
- Services already expose `GET /api/Home/health`.
- Services already use Serilog in `Program.cs`, but there is no centralized sink.
- No OpenTelemetry packages or Prometheus integration are currently wired.

This means the best approach is to start with cluster-level observability and small, additive application changes.

---

## Best-Fit Approach for This Repo

### Recommendation

Create a **new ArgoCD "observability" app** for dev that deploys:

1. `kube-prometheus-stack` (Prometheus + Alertmanager + Grafana)
2. `loki` (+ `promtail` or Grafana Alloy) for logs

Then add lightweight instrumentation in the services:
- `/metrics` endpoint (Prometheus/OpenTelemetry metrics)
- optional structured request logging enrichment (trace/request ids)
- health probes in service Helm template

### Why this is best here

- Matches your current GitOps model (ArgoCD app-per-domain).
- Gives value quickly (dashboards and alerts) before deep code changes.
- Limits blast radius: cluster stack first, app changes second, traces third.
- Works with your existing `api/Home/health` endpoint and current Serilog usage.

---

## Minimum Setup You Suggested (Grafana dashboards for metrics/health/errors/logs)

For your case, this should be the **true minimum**:

1. Grafana + Prometheus
2. Loki (yes, include logs from day 1)
3. Dashboards:
   - service health and probe status
   - request rate / latency / error rate (RED)
   - pod restarts, CPU/memory saturation
   - error logs by service and exception type

Without logs, "errors" panels become weak and incident analysis is slower. So logs should be in the minimum baseline.

---

## Rollout Plan (Low Risk)

### Phase 0: Platform app only

- Add `helm/argocd-applications/dev/observability-app.yaml`.
- Point it to a new chart path, for example `helm/observability`.
- Deploy Grafana + Prometheus + Loki stack.
- Import base dashboards (Kubernetes + .NET + service overview).

No application code changes yet.

### Phase 1: Helm/service-template hardening

Update `helm/service-template`:
- add `livenessProbe`, `readinessProbe`, `startupProbe` -> `/api/Home/health`
- add optional pod annotations for Prometheus scraping
- keep everything behind values flags to avoid breaking old releases

Result: health + pod visibility immediately in Grafana.

### Phase 2: Metrics from services

Add metrics endpoint to the service template startup path:
- expose `/metrics` (Prometheus-net or OpenTelemetry metrics exporter)
- collect HTTP server metrics and dependency metrics

Start with one pilot service (Identity or Wallet), then roll out to all.

### Phase 3: Better correlation and traces (recommended after baseline)

- Introduce OpenTelemetry tracing in shared startup wiring.
- Instrument ASP.NET Core + HttpClient first.
- Add Kafka trace header propagation in `Defender.Kafka` wrappers as backward-compatible behavior.

This phase closes the H4 gap fully for cross-service debugging.

---

## Proposed Dashboard Set (Grafana)

1. **Service Overview**
   - RPS, p95 latency, 5xx rate by service
   - top endpoints by latency

2. **Health & Availability**
   - readiness/liveness status by pod
   - restart count
   - deployment rollout status

3. **Errors**
   - exception count by service/type
   - failed requests by endpoint
   - last 50 critical logs

4. **Infrastructure**
   - CPU/memory per pod
   - throttling and OOM indicators

5. **Kafka (when added)**
   - consumer lag
   - produce/consume error count
   - throughput per topic

---

## Concrete Repo Changes to Plan Next

1. Add new folder `helm/observability/` with values for dev.
2. Add new ArgoCD app manifest for observability in `helm/argocd-applications/dev`.
3. Extend `helm/service-template/values.yaml` with probe and scrape toggles.
4. Extend `helm/service-template/templates/deployment.yaml` to use those toggles.
5. Add shared app-level metrics wiring in service startup (feature-flagged).

---

## Decision

For this monorepo, the best approach is:

- **Now:** Grafana + Prometheus + Loki via a dedicated ArgoCD observability app, plus probe/scrape support in the service template.
- **Next:** Add `/metrics` in all services using shared startup conventions.
- **Then:** Add OpenTelemetry tracing (HTTP first, Kafka next).

This gives immediate operational visibility with low migration risk and no "big bang" rewrite.
