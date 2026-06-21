# ArgoCD Deployment Guide (Current)

This document reflects the current ArgoCD setup in this repository.

## Scope

- Service deployments are managed as ArgoCD Applications using Helm.
- Service chart: `helm/service-template`
- Application manifests: `helm/argocd-applications/<env>/`

## Current Repository State

### ArgoCD Application Manifests

- `helm/argocd-applications/dev/*.yaml` for service apps
- Observability ArgoCD Applications are currently disabled and are not included in the app-of-apps manifests.

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

## Image Promotion Flow

ArgoCD deploys the image tag pinned in git. The deployment template renders:

```yaml
image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
```

So the effective deployed version comes from the service values file, for example:

```yaml
# helm/service-template/values-portal.yaml
image:
  repository: defendersd/defender.portal
  tag: 20260620-100
```

The Docker build workflow publishes images and tags, but it does not choose the deployed tag.
Service image tags are promoted into `helm/service-template/values-*.yaml` via:

- `.github/workflows/promote-image-tag.yml`

After value files are updated in git, ArgoCD sync applies the change.
ArgoCD can take up to 3 minutes after the promotion commit to detect the new git state and deploy the new image tag.

When a PR changes multiple deployable services, promote each affected service. For example, a
feature that changes both `Defender.Portal` and `Defender.HealthCareService` needs one promotion
for `values-portal.yaml` and one promotion for `values-health-care.yaml`.

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
