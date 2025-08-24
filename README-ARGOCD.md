# ArgoCD Automation for Defender.MonoRepo

This document describes how to use the automated ArgoCD deployment system for the Defender.MonoRepo project.

## Overview

The system provides automated deployment to ArgoCD when new Docker images are published. It supports multiple deployment strategies and can deploy specific services or all services at once.

## Architecture

```
GitHub Actions (Build) → Docker Registry → ArgoCD Deployment → Kubernetes Cluster
```

### Components

1. **Docker Build Workflow** (`.github/workflows/docker-build-publish.yml`)
   - Builds and publishes Docker images for all services
   - Triggers ArgoCD deployment workflow on completion

2. **ArgoCD Deployment Workflows**
   - `argocd-deploy.yml`: Basic deployment workflow
   - `argocd-tagged-deploy.yml`: Advanced deployment with strategies

3. **ArgoCD Application Scripts**
   - `scripts/generate-argocd-apps.sh`: Generates basic ArgoCD applications
   - `scripts/create-tagged-deployment.sh`: Creates deployment strategies

4. **Helm Charts**
   - `helm/service-template/`: Reusable Helm chart for all services
   - `helm/argocd-applications/`: Generated ArgoCD application manifests

## Prerequisites

### 1. ArgoCD Cluster Setup

Ensure you have ArgoCD running in your Kubernetes cluster:

```bash
# Install ArgoCD (if not already installed)
kubectl create namespace argocd
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml

# Get ArgoCD admin password
kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}" | base64 -d
```

### 2. GitHub Secrets Configuration

Add the following secrets to your GitHub repository:

- `ARGOCD_SERVER`: Your ArgoCD server URL (e.g., `argocd.your-domain.com`)
- `ARGOCD_AUTH_TOKEN`: ArgoCD authentication token
- `KUBECONFIG`: Base64-encoded kubeconfig file for your cluster

#### Getting ArgoCD Auth Token

```bash
# Login to ArgoCD
argocd login <your-argocd-server>

# Create a service account token
argocd account generate-token --account <service-account-name>
```

#### Getting Kubeconfig

```bash
# Get your kubeconfig and encode it
kubectl config view --raw | base64 -w 0
```

## Usage

### Automatic Deployment

The system automatically deploys when:
1. Docker images are built and published successfully
2. The workflow runs on `main` or `develop` branches
3. New tags are pushed

### Manual Deployment

You can manually trigger deployments using the workflow dispatch:

1. Go to **Actions** → **ArgoCD Tagged Deployment**
2. Click **Run workflow**
3. Configure the deployment:
   - **Service**: Specific service or leave empty for all
   - **Image Tag**: Specific tag or leave empty for latest
   - **Deployment Strategy**: staging, production, tagged, or all

### Deployment Strategies

#### 1. Staging (`staging`)
- Deploys to `defender-staging` namespace
- Uses `staging` ArgoCD project
- Suitable for pre-production testing

#### 2. Production (`production`)
- Deploys to `defender` namespace
- Uses `production` ArgoCD project
- For production deployments

#### 3. Tagged (`tagged`)
- Deploys to `defender-tagged` namespace
- Uses `default` ArgoCD project
- For specific version testing

#### 4. All (`all`)
- Creates applications for all strategies
- Useful for initial setup

## Service Mapping

| Service Name | ArgoCD App Name | Values File |
|--------------|-----------------|-------------|
| Defender.Portal | defender-portal | values-portal.yaml |
| Defender.UserManagementService | defender-user-management | values-user-management.yaml |
| Defender.WalletService | defender-wallet | values-wallet.yaml |
| Defender.RiskGamesService | defender-risk-games | values-risk-games.yaml |
| Defender.NotificationService | defender-notification | values-notification.yaml |
| Defender.JobSchedulerService | defender-job-scheduler | values-job-scheduler.yaml |
| Defender.IdentityService | defender-identity | values-identity.yaml |

## Workflow Triggers

### 1. Automatic Trigger
```yaml
on:
  workflow_run:
    workflows: ["Build and Publish Docker Images"]
    types: [completed]
    branches: [main, develop]
```

### 2. Manual Trigger
```yaml
on:
  workflow_dispatch:
    inputs:
      service:
        description: 'Specific service to deploy'
        required: false
      image_tag:
        description: 'Specific image tag to deploy'
        required: false
      deployment_strategy:
        description: 'Deployment strategy'
        type: choice
        options: [staging, production, tagged, all]
```

## File Structure

```
├── .github/workflows/
│   ├── docker-build-publish.yml          # Builds Docker images
│   ├── argocd-deploy.yml                # Basic ArgoCD deployment
│   └── argocd-tagged-deploy.yml         # Advanced deployment strategies
├── helm/
│   ├── service-template/                 # Reusable Helm chart
│   │   ├── Chart.yaml
│   │   ├── values-*.yaml                # Service-specific values
│   │   └── templates/                    # Kubernetes manifests
│   └── argocd-applications/             # Generated ArgoCD apps
│       ├── defender-portal-app.yaml
│       ├── defender-user-management-app.yaml
│       └── ...
└── scripts/
    ├── generate-argocd-apps.sh          # Basic app generation
    └── create-tagged-deployment.sh       # Strategy-based deployment
```

## Customization

### 1. Adding New Services

1. Add the service to the matrix in `docker-build-publish.yml`
2. Create a values file in `helm/service-template/`
3. Update the service mapping in the scripts

### 2. Modifying Deployment Strategies

Edit `scripts/create-tagged-deployment.sh` to:
- Add new deployment types
- Modify namespace patterns
- Change ArgoCD project assignments

### 3. Helm Chart Customization

Modify `helm/service-template/templates/` to:
- Add new Kubernetes resources
- Customize resource limits
- Add environment-specific configurations

## Monitoring and Troubleshooting

### 1. ArgoCD UI

Access your ArgoCD UI to monitor:
- Application sync status
- Deployment history
- Resource health

### 2. Workflow Logs

Check GitHub Actions logs for:
- Deployment progress
- Error messages
- Sync status

### 3. Common Issues

#### Authentication Errors
- Verify `ARGOCD_AUTH_TOKEN` is correct
- Check if the token has expired
- Ensure proper permissions

#### Sync Failures
- Check Kubernetes cluster connectivity
- Verify Helm chart syntax
- Check resource quotas and limits

#### Image Pull Errors
- Verify Docker registry credentials
- Check image tag existence
- Ensure proper image pull policy

## Security Considerations

### 1. Access Control
- Use ArgoCD projects to limit access
- Implement RBAC for different environments
- Use service accounts with minimal permissions

### 2. Secrets Management
- Store sensitive data in Kubernetes secrets
- Use ArgoCD's built-in secret management
- Consider external secret operators

### 3. Network Policies
- Implement network policies for service isolation
- Use ingress controllers with proper TLS
- Restrict pod-to-pod communication

## Best Practices

### 1. Deployment Strategy
- Use staging for testing before production
- Implement blue-green or canary deployments
- Set appropriate sync windows

### 2. Resource Management
- Set resource limits and requests
- Use horizontal pod autoscaling
- Monitor resource usage

### 3. Monitoring
- Implement health checks
- Use ArgoCD's built-in monitoring
- Set up alerts for failures

## Support

For issues or questions:
1. Check the workflow logs in GitHub Actions
2. Review ArgoCD application status
3. Check Kubernetes cluster events
4. Review the generated ArgoCD application manifests

## Contributing

When contributing to this automation:
1. Test changes in a staging environment
2. Update documentation for new features
3. Follow the existing code patterns
4. Add appropriate error handling
