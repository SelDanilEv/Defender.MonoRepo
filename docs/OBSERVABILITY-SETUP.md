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

## 5. Production Hardening Checklist

Before enabling in prod, replace defaults in [`helm/observability/values-prod.yaml`](../helm/observability/values-prod.yaml):

- `grafana.adminPassword`
- Grafana ingress host/domain
- storage sizes and storage class settings
- alert routing and receiver configs
