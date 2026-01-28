# Local services

From `src/`, use Docker Compose profiles:

Local:

```
docker compose --profile local -p local up -d --build
```

Dev:

```
docker compose --profile dev -p local up -d --build
```

Use `-p local` for dev to keep existing volumes (Mongo/Postgres).

Stop and remove containers:

```
docker compose -p local down
```
