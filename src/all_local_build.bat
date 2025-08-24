@echo off
docker-compose -f local-docker-compose.yml -p local %* build --progress=plain --no-cache