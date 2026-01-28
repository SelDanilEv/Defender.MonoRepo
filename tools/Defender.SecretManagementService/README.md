Defender Secret Management Service

Service to manage my mongo secrets

## Local usage

### Prerequisites
- Docker Desktop (or compatible Docker runtime)
- External Docker network named `external-network`

### Secrets
Secrets are loaded from profile-specific files:
- `secrets/secrets.local.list`
- `secrets/secrets.dev.list`
- `secrets/secrets.prod.list`

### Docker Compose (profiles)
Use the single compose file with profiles:
- Local: `docker compose -f docker-compose.yml --profile local up --build`
- Dev: `docker compose -f docker-compose.yml --profile dev up --build`
- Prod: `docker compose -f docker-compose.yml --profile prod up --build`

### Deploy script
The helper script wraps the compose commands:
- `./deploy.sh local`
- `./deploy.sh dev up`
- `./deploy.sh prod down`
