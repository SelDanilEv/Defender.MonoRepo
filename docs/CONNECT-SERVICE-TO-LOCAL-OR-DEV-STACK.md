# Connect Any Service To Local Or Dev Stack

This guide explains how to run one service from Visual Studio on your host machine and connect it to the rest of the Defender stack running in Docker.

## Goal

- Run one service locally from Visual Studio.
- Keep the rest of services in Docker (`local` or `dev` profile).
- Avoid port conflicts.
- Make service-to-service calls work in both directions.

## Prerequisites

- Docker Desktop is running.
- Stack is in `C:\MyApps\Defender.MonoRepo`.
- You know the service host port from `src/docker-compose.yml`.

## Required Secrets (common)

Most services require these environment variables:

- `Defender_App_JwtSecret`
- `Defender_App_MongoDBConnectionString`

Some services require extra secrets. Example for Personal Food Adviser:

- `Defender_App_HuggingFaceApiKey`

Secret name prefix is `Defender_App_` (`src/Defender.Common/src/Defender.Common/Helpers/SecretsHelper.cs`).

## Local Stack (recommended for local debugging)

### 1. Start local stack

```powershell
docker compose -f src/docker-compose.yml --profile local up -d
```

### 2. Stop only the service you will run from Visual Studio

Replace `<compose-service-name>` with the service key from `src/docker-compose.yml`.

```powershell
docker compose -f src/docker-compose.yml --profile local stop <compose-service-name>
```

Example:

```powershell
docker compose -f src/docker-compose.yml --profile local stop local-personal-food-adviser-service
```

### 3. Run service from Visual Studio on the same port

- Open service project.
- Select the project launch profile (`WebApi` or service profile).
- Keep the same local URL/port as compose mapping (example: `http://localhost:47062`).

If port is in use, the container for that service is still running.

### 4. Use host-reachable dependency endpoints

When service runs on host, dependencies should be host reachable:

- Kafka: `localhost:9092`
- Mongo: `localhost:27017`
- Postgres: `localhost:5432`

Use service `appsettings.Debug.json` when available, or override with env vars:

```powershell
$env:KafkaOptions__BootstrapServers="localhost:9092"
```

### 5. Make Docker services call your host-run service

If a Dockerized caller (example: Portal) needs your host-run service, its local config should use:

- `http://host.docker.internal:<your-port>/`

Example already used by Portal local config:

- `src/Defender.Portal/src/WebUI/appsettings.Local.json`

### 6. Verify

- Check service logs in Visual Studio.
- Check caller container logs:

```powershell
docker compose -f src/docker-compose.yml --profile local logs --tail=200 <caller-service-name>
```

## Dev Stack

Use this when you specifically need `Dev` profile behavior or remote dev endpoints.

### 1. Start dev stack

```powershell
docker compose -f src/docker-compose.yml --profile dev up -d
```

### 2. Stop the target dev container

```powershell
docker compose -f src/docker-compose.yml --profile dev stop <compose-service-name>
```

### 3. Run service from Visual Studio with `ASPNETCORE_ENVIRONMENT=Dev`

- Select launch profile or set environment variable.
- Ensure the service listens on expected local port.

### 4. Caller routing

If Dockerized services in `dev` profile must call your host-run service, point their URL to:

- `http://host.docker.internal:<your-port>/`

## Kafka Prefix Note

Topic prefixes depend on environment name (`src/Defender.Kafka/src/Defender.Kafka/Service/KafkaEnvPrefixer.cs`):

- `Local` or `DockerLocal` => `local_`
- `Dev` or `DockerDev` => `dev_`
- `Debug` falls back to `local_`

If your service runs as `Debug`, it will publish/consume `local_` topics.

## Troubleshooting

- `Address already in use`: stop the same service container in compose.
- Service can call others, but others cannot call service: update caller URL to `host.docker.internal`.
- Kafka timeout:
  - verify `KafkaOptions:BootstrapServers`
  - verify Kafka container health
  - verify topic leader exists (no `Leader: none`)
- 401/Invalid JWT: ensure `Defender_App_JwtSecret` matches stack secret.

## Personal Food Adviser quick example

```powershell
# stack up
docker compose -f src/docker-compose.yml --profile local up -d

# free port 47062
docker compose -f src/docker-compose.yml --profile local stop local-personal-food-adviser-service

# run WebApi profile from Visual Studio (http://localhost:47062)
# use Debug env so Kafka is localhost:9092 in appsettings.Debug.json
```
