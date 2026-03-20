# ArgoCD Deployment Guide (Current)

This document reflects the current ArgoCD setup in this repository.

## Scope

- Service deployments are managed as ArgoCD Applications using Helm.
- Service chart: `helm/service-template`
- Observability chart: `helm/observability`
- Application manifests: `helm/argocd-applications/`

## Current Repository State

### ArgoCD Application Manifests

- `helm/argocd-applications/*.yaml` for service apps
- Observability is not currently deployed through ArgoCD in this repository

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

## Deploying Service Apps

Example:

```bash
kubectl apply -f helm/argocd-applications/identity-app.yaml -n argocd
```

Apply all service apps (PowerShell):

```powershell
Get-ChildItem helm/argocd-applications/*.yaml |
  ForEach-Object { kubectl apply -f $_.FullName -n argocd }
```

Observability is still defined as a Helm chart in `helm/observability`, but its ArgoCD application manifest is intentionally not present right now.

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
