# Defender Operations Guide

Current guide for image promotion, ArgoCD, and observability. Service workloads use
`helm/service-template`; ArgoCD Applications live under `helm/argocd-applications/<env>/`.

## Deployment flow

Current workflows:

- `.github/workflows/docker-build-publish.yml` builds, tests, and publishes images.
- `.github/workflows/promote-image-tag.yml` writes selected image tags into
  `helm/service-template/values-*.yaml`.

ArgoCD deploys the tag pinned in Git:

```yaml
image:
  repository: defendersd/defender.portal
  tag: 20260620-100
```

Image publication alone does not deploy that image. Run promotion for every changed deployable
service, wait for the promotion commit, then allow up to three minutes for ArgoCD detection and
sync.

Apply project configuration before Applications:

```powershell
kubectl apply -f helm/argocd-config/argocd-projects.yaml -n argocd
kubectl apply -f helm/argocd-applications/dev/identity-app.yaml -n argocd
```

Apply all enabled Dev service Applications:

```powershell
Get-ChildItem helm/argocd-applications/dev/*.yaml |
  Where-Object { $_.Name -ne "observability-app.yaml" } |
  ForEach-Object { kubectl apply -f $_.FullName -n argocd }
```

Legacy `argocd-deploy.yml`, `argocd-tagged-deploy.yml`, and
`create-tagged-deployment.sh` flows are not part of current repository state.

### Deployment validation

1. ArgoCD project permits destination namespace.
2. Application uses correct chart path and values file.
3. `helm template` renders with same values.
4. ArgoCD reports `Synced` and `Healthy`.
5. Live workload uses promoted tag and health route responds.

Troubleshooting:

- `PermissionDenied`: reapply `helm/argocd-config/argocd-projects.yaml`.
- `Path not found`: verify Application `spec.source.path`.
- Render failure: reproduce with local `helm template` and exact values.

## Docker observability

All `debug`, `local`, and `dev` profiles provide:

- Prometheus: `http://localhost:9090`
- Loki: `http://localhost:3100`
- Promtail
- Grafana: `http://localhost:3000` (`admin` / `admin` for local use)

Start only observability or full stacks:

```powershell
docker compose -f src/docker-compose.yml --profile debug up -d local-prometheus local-loki local-promtail local-grafana
docker compose -f src/docker-compose.yml --profile local up -d --build
docker compose -f src/docker-compose.yml --profile dev up -d --build
```

Grafana loads dashboards from `src/observability/grafana/provisioning/dashboards/json`:

- Defender Overview
- Defender RED Metrics
- Defender Runtime Health
- Defender Errors & Logs

Overview panels include availability, RPS, 5xx rate, p95 latency, CPU/memory pressure, and logs.
Web entrypoints expose `/metrics` when
`Defender__Observability__Metrics__Enabled=true`; compose and Helm defaults enable it.

## Kubernetes observability

`helm/observability` contains kube-prometheus-stack, Loki, and Promtail. Dashboards ship from
`helm/observability/dashboards`. Stable datasource UIDs are Prometheus `P000000001` and Loki
`L000000001`.

Observability Applications are currently excluded from app-of-apps. Enable explicitly only where
needed. If ArgoCD has production Applications only, skip Dev observability.

```powershell
kubectl apply -f helm/argocd-config/argocd-projects.yaml -n argocd
kubectl apply -f helm/argocd-applications/prod/observability-app.yaml -n argocd
# Optional:
kubectl apply -f helm/argocd-applications/dev/observability-app.yaml -n argocd
```

Applications use `ServerSideApply=true`, `Replace=true`, and
`SkipDryRunOnMissingResource=true` for CRDs and first installation.

Before production enablement, replace Grafana password and ingress host, choose storage classes and
sizes, and configure alert receivers. Default and Dev Loki use ephemeral `/tmp/loki`; production
uses persistent `/var/loki` with PVC enabled.
