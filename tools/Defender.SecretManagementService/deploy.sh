#!/usr/bin/env sh
set -eu

usage() {
  echo "Usage: ./deploy.sh <local|dev|prod> [up|down|restart|logs|pull]"
  exit 1
}

PROFILE="${1:-}"
ACTION="${2:-up}"

case "$PROFILE" in
  local|dev|prod) ;;
  *) usage ;;
esac

COMPOSE_FILE="./docker-compose.yml"

case "$ACTION" in
  up)
    docker compose -f "$COMPOSE_FILE" --profile "$PROFILE" up --build -d
    ;;
  down)
    docker compose -f "$COMPOSE_FILE" --profile "$PROFILE" down
    ;;
  restart)
    docker compose -f "$COMPOSE_FILE" --profile "$PROFILE" down
    docker compose -f "$COMPOSE_FILE" --profile "$PROFILE" up --build -d
    ;;
  logs)
    docker compose -f "$COMPOSE_FILE" --profile "$PROFILE" logs -f
    ;;
  pull)
    docker compose -f "$COMPOSE_FILE" --profile "$PROFILE" pull
    ;;
  *)
    usage
    ;;
esac
