# Docker Build and Publish Workflow

This GitHub Action workflow automatically builds and publishes Docker images for all Defender services to Docker Hub.

## Features

- **Matrix Build**: Builds all services in parallel for faster execution
- **Smart Tagging**: Automatically creates appropriate tags based on git events
- **Manual Trigger**: Can build specific services on demand
- **NuGet Egress Preflight**: Verifies outbound HTTPS to NuGet before `dotnet test`
- **Caching**: Uses GitHub Actions cache for faster builds
- **Security gates for pull requests**: Gitleaks secret scanning, NuGet and npm dependency audits, Helm lint/template validation, Trivy configuration scans, and Trivy scans of each built service image. High and critical findings fail the workflow; no baseline exceptions are currently configured.
- **Security**: Only pushes images on main/develop branches and tags (not on PRs)

## Services Built

The workflow automatically builds the following services:

- `Defender.Portal` (uses Dockerfile.Portal)
- `Defender.UserManagementService` (uses Dockerfile.Service)
- `Defender.WalletService` (uses Dockerfile.Service)
- `Defender.RiskGamesService` (uses Dockerfile.Service)
- `Defender.NotificationService` (uses Dockerfile.Service)
- `Defender.PersonalFoodAdvisor` (uses Dockerfile.Service)
- `Defender.JobSchedulerService` (uses Dockerfile.Service)
- `Defender.IdentityService` (uses Dockerfile.Service)
- `Defender.BudgetTracker` (uses Dockerfile.Service)
- `Defender.HealthCareService` (uses Dockerfile.Service)

## Triggers

### Automatic Triggers
- **Push to main/develop**: Builds and publishes all services
- **Git tags (v*)**: Builds and publishes all services with version tags
- **Pull requests**: Builds all services (doesn't publish)

### Manual Trigger
- **Workflow dispatch**: Can build specific services on demand

## Required Secrets

Add these secrets to your GitHub repository:

1. **DOCKERHUB_USERNAME**: Your Docker Hub username
2. **DOCKERHUB_TOKEN**: Your Docker Hub access token (not password)

### Setting up Docker Hub Token

1. Go to [Docker Hub](https://hub.docker.com/)
2. Sign in to your account
3. Go to Account Settings → Security
4. Create a new access token
5. Copy the token and add it to your GitHub repository secrets

## Tagging Strategy

The workflow automatically creates tags based on the git event:

- **Branch pushes**: `branch-name`, `branch-name-sha`
- **Tags**: `v1.0.0`, `1.0`
- **Default branch**: `latest`
- **Pull requests**: `pr-123-sha`

## Deployment Tag Promotion

Building and publishing an image does not update what ArgoCD deploys by itself. ArgoCD renders
`helm/service-template` and uses an immutable image reference from the matching `values-*.yaml` file. Set `digest` to render `repository@sha256:...`; omit it only for services still using the existing pinned-tag behavior:

```yaml
image:
  repository: defendersd/defender.portal
  tag: 20260620-100
  digest: sha256:<resolved-manifest-digest>
```

After a successful build, run `Promote Image Tag` for every deployable service changed by the PR.
For example, if a change touches both `Defender.HealthCareService` and `Defender.Portal`, promote
both services to their newly published build tags. Resolve and commit an image digest when promoting
a service that requires immutable delivery. The promote workflow commits the updated
`helm/service-template/values-*.yaml` file, and ArgoCD deploys that pinned reference from git.

## Usage Examples

### Build All Services
```bash
# Push to main branch
git push origin main

# Create a version tag
git tag v1.0.0
git push origin v1.0.0
```

### Build Specific Service
1. Go to Actions tab in GitHub
2. Select "Build and Publish Docker Images"
3. Click "Run workflow"
4. Enter the service name (e.g., `Defender.Portal`)
5. Click "Run workflow"

### Promote Built Image For Deployment
1. Open the successful "Build and Publish Docker Images" run.
2. Find the published tag for each changed service, usually `YYYYMMDD-<run-number>`.
3. Select "Promote Image Tag".
4. Enter the same service name and image tag.
5. Confirm that the workflow commits the matching `helm/service-template/values-*.yaml` update.

## Image Names

Images will be published to Docker Hub with the following naming convention:
```
docker.io/{DOCKERHUB_USERNAME}/Defender.{ServiceName}:{tag}
```

Example:
```
docker.io/myusername/Defender.Portal:latest
docker.io/myusername/Defender.UserManagementService:v1.0.0
```

## Build Context

The workflow uses the `./src` directory as the build context, which contains:
- All service source code
- Dockerfiles
- Common dependencies
- Build configuration files

## Performance Features

- **Parallel builds**: All services build simultaneously
- **Layer caching**: Uses GitHub Actions cache for faster rebuilds
- **Multi-stage builds**: Leverages Docker multi-stage builds for smaller images
- **Alpine base**: Uses lightweight Alpine Linux base images

## Troubleshooting

### Common Issues

1. **Authentication failed**: Check your Docker Hub credentials in repository secrets
2. **Build context error**: Ensure all required files are in the `./src` directory
3. **Service not found**: Verify the service name matches exactly (case-sensitive)
4. **NuGet restore fails (NU1301)**: Allow outbound HTTPS to `*.nuget.org` on port `443`

### Debug Mode

To debug build issues:
1. Check the workflow logs in the Actions tab
2. Verify the Dockerfile paths and build arguments
3. Ensure all required dependencies are available in the build context

## Customization

### Adding New Services

To add a new service, update the matrix in the workflow:

```yaml
- name: 'Defender.NewService'
  dockerfile: 'Dockerfile.Service'  # or Dockerfile.Portal
  project_type: 'WebApi'            # or WebUI
```

### Modifying Build Arguments

Update the `build-args` section in the workflow to pass additional arguments to your Dockerfiles.

### Changing Base Images

Update the `DOTNET_VERSION` environment variable or modify the Dockerfiles directly.
