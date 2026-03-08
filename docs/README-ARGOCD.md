# ArgoCD Deployment Guide (Current)

This document reflects the current ArgoCD setup in this repository.

## Scope

- Service deployments are managed as ArgoCD Applications using Helm.
- Service chart: `helm/service-template`
- Observability chart: `helm/observability`
- Application manifests: `helm/argocd-applications/<env>/`

## Current Repository State

### ArgoCD Application Manifests

- `helm/argocd-applications/dev/*.yaml` for service apps
- `helm/argocd-applications/dev/observability-app.yaml`
- `helm/argocd-applications/prod/observability-app.yaml`

### ArgoCD Project Configuration

- `helm/argocd-config/argocd-projects.yaml`
- Includes project `observability` for observability namespaces and `kube-system` destination used by kube-prometheus-stack resources.

### Workflows

Current workflows in this repo:

- `.github/workflows/docker-build-publish.yml`
- `.github/workflows/promote-image-tag.yml`

Note: legacy docs/scripts that mention `argocd-deploy.yml`, `argocd-tagged-deploy.yml`, or `create-tagged-deployment.sh` are not part of the current repository state.

## Deploying ArgoCD Config

Apply ArgoCD projects first:

```bash
kubectl apply -f helm/argocd-config/argocd-projects.yaml -n argocd
```

## Deploying Service Apps (Dev)

Example:

```bash
kubectl apply -f helm/argocd-applications/dev/identity-app.yaml -n argocd
```

Apply all dev service apps (PowerShell):

```powershell
Get-ChildItem helm/argocd-applications/dev/*.yaml |
  Where-Object { $_.Name -ne "observability-app.yaml" } |
  ForEach-Object { kubectl apply -f $_.FullName -n argocd }
```

## Deploying Observability Apps

Prod (primary):

```bash
kubectl apply -f helm/argocd-applications/prod/observability-app.yaml -n argocd
```

Dev (optional):

```bash
kubectl apply -f helm/argocd-applications/dev/observability-app.yaml -n argocd
```

For full observability details, see `docs/OBSERVABILITY-SETUP.md`.

Observability apps include sync options tuned for operator/CRD workloads:

- `ServerSideApply=true`
- `Replace=true`
- `SkipDryRunOnMissingResource=true`

## Image Promotion Flow

Service image tags are updated in `helm/service-template/values-*.yaml` via:

- `.github/workflows/promote-image-tag.yml`

After value files are updated in git, ArgoCD sync applies the change.

## Validation Checklist

1. ArgoCD project exists and permits target namespace.
2. Application points to correct chart path and values file.
3. Helm template renders locally without errors.
4. ArgoCD application sync status is `Synced` and health is `Healthy`.

## Troubleshooting

- `PermissionDenied`/project errors:
  - Re-apply `helm/argocd-config/argocd-projects.yaml`
- `Path not found`:
  - Verify `spec.source.path` in the app manifest.
- Helm render failures:
  - Run `helm template` locally with the same value files.
