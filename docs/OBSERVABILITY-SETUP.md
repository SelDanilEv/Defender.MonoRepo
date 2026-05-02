# Defender Observability Setup

This guide implements the observability rollout plan for all runtime environments:

- Docker `debug`
- Docker `local`
- Docker `dev`
- ArgoCD `prod` (cloud cluster)
- ArgoCD `dev` (optional)

## ArgoCD Scope (Important)

If your ArgoCD instance has only production applications, deploy only the prod observability app and skip all dev ArgoCD steps.

## 1. Docker Environments (`debug`, `local`, `dev`)

Observability containers are added to [`src/docker-compose.yml`](../src/docker-compose.yml):

- `local-prometheus` at `http://localhost:9090`
- `local-loki` at `http://localhost:3100`
- `local-promtail`
- `local-grafana` at `http://localhost:3000` (admin/admin)

All observability services are available in profiles: `debug`, `local`, `dev`.

### Provisioned dashboards (built-in in this repo)

Grafana auto-loads dashboards from `src/observability/grafana/provisioning/dashboards/json`:

- `Defender Overview`
- `Defender RED Metrics`
- `Defender Runtime Health`
- `Defender Errors & Logs`

`Defender Overview` includes critical summary panels:

- availability (%)
- total RPS
- 5xx error %
- p95 latency
- per-job request rate
- per-job CPU and memory pressure
- error logs and all service logs

### Run only observability (debug mode)

```bash
docker compose -f src/docker-compose.yml --profile debug up -d local-prometheus local-loki local-promtail local-grafana
```

### Run full local stack + observability

```bash
docker compose -f src/docker-compose.yml --profile local up -d --build
```

### Run full dev stack + observability

```bash
docker compose -f src/docker-compose.yml --profile dev up -d --build
```

## 2. Service Metrics Endpoint

All web entrypoints now support `/metrics` with `prometheus-net` when the feature flag is enabled:

- `Defender__Observability__Metrics__Enabled=true`

This flag is enabled in:

- Docker local/dev compose env
- Helm service template config map defaults

## 3. Kubernetes/ArgoCD

New Helm chart: [`helm/observability`](../helm/observability)

Includes:

- `kube-prometheus-stack` (Prometheus + Alertmanager + Grafana)
- `loki`
- `promtail`

ArgoCD applications:

- Prod (primary): [`helm/argocd-applications/prod/observability-app.yaml`](../helm/argocd-applications/prod/observability-app.yaml)
- Dev (optional): [`helm/argocd-applications/dev/observability-app.yaml`](../helm/argocd-applications/dev/observability-app.yaml)

ArgoCD project:

- [`helm/argocd-config/argocd-projects.yaml`](../helm/argocd-config/argocd-projects.yaml) adds project `observability`.
- The project targets only dedicated observability namespaces to keep Argo permissions minimal and predictable.

### What is provisioned by default in Kubernetes

Grafana dashboards are shipped from [`helm/observability/dashboards`](../helm/observability/dashboards) and auto-imported by sidecar:

- `Defender Overview` (extended with restart/OOM/throttling/memory pressure)
- `Defender RED Metrics`
- `Defender Runtime Health`
- `Defender Errors & Logs`

Grafana datasources are provisioned with stable UIDs for dashboard compatibility:

- Prometheus `P000000001`
- Loki `L000000001`

## 4. Deploy with ArgoCD

1. Apply/update ArgoCD project config:

```bash
kubectl apply -f helm/argocd-config/argocd-projects.yaml -n argocd
```

2. Apply prod observability application:

```bash
kubectl apply -f helm/argocd-applications/prod/observability-app.yaml -n argocd
```

3. Optional: apply dev observability application:

```bash
kubectl apply -f helm/argocd-applications/dev/observability-app.yaml -n argocd
```

Both observability apps are configured with ArgoCD sync options required for large CRDs and first-time operator installs:

- `ServerSideApply=true`
- `Replace=true`
- `SkipDryRunOnMissingResource=true`

## 5. Production Hardening Checklist

Before enabling in prod, replace defaults in [`helm/observability/values-prod.yaml`](../helm/observability/values-prod.yaml):

- `grafana.adminPassword`
- Grafana ingress host/domain
- storage sizes and storage class settings
- alert routing and receiver configs

Loki storage behavior:

- Default/dev values use ephemeral filesystem storage under `/tmp/loki` (free-tier friendly, no PVC requirement).
- Prod values override Loki to persistent `/var/loki` with PVC enabled.
