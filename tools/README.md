# Tools README

Этот каталог содержит вспомогательные инструменты для локальной работы с секретами и Mongo-данными.

## AI agent quick path

Если цель — быстро поднять локальный стек и убрать блокеры, обычно лучший порядок такой:

1. Проверить Docker и Mongo.
2. Поднять `Defender.SecretManagementService`.
3. Проверить, есть ли `JwtSecret` через его API.
4. Если секрета нет в Mongo — использовать `Defender.SimpleMongoMigrator`.
5. Перезапустить `Portal` и `UserManagementService`.
6. Если падает `TravelCalendarService` на старой схеме — очистить только локальную `local_Defender_TravelCalendarService`.

Это самый полезный практический сценарий для локального AI-assisted troubleshooting в этой репе.

## Состав

- `Defender.SecretManagementService` — локальный API для чтения и записи секретов в Mongo.
- `Defender.SimpleMongoMigrator` — консольный мигратор Mongo между `local/dev/prod`.

## 1. Defender.SecretManagementService

Назначение:

- читать секреты через HTTP API;
- смотреть список ключей;
- создавать, обновлять и удалять секреты в Mongo.

Где лежат локальные секреты:

- `tools/Defender.SecretManagementService/secrets/secrets.local.list`
- `tools/Defender.SecretManagementService/secrets/secrets.dev.list`
- `tools/Defender.SecretManagementService/secrets/secrets.prod.list`

Для локального запуска нужны:

- Docker Desktop;
- сеть Docker `external-network`;
- доступный Mongo на `localhost:27017` или через `Defender_App_MongoDBConnectionString`.

Запуск в Docker:

```powershell
docker compose -f tools/Defender.SecretManagementService/docker-compose.yml --profile local up -d --build
```

Порт:

- `47056`

Проверка health:

```powershell
Invoke-WebRequest -UseBasicParsing http://127.0.0.1:47056/api/home/health
```

Полезные endpoint'ы:

- получить значение секрета:

```powershell
Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:47056/api/secret/getValue?secretName=JwtSecret"
```

- получить список ключей:

```powershell
Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:47056/api/secret/get-all/keys?page=1&pageSize=50"
```

- получить полный документ секрета:

```powershell
Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:47056/api/secret/get?secretName=JwtSecret"
```

Важно:

- если `get-all/keys` пустой, это значит, что Mongo-хранилище секретов пустое;
- наличие `Defender_App_JwtSecret` в `secrets.local.list` не означает, что секрет уже лежит в Mongo;
- часть сервисов в монорепе читает `JwtSecret` через `SecretsHelper(..., useMongo: true)`, поэтому одного env-файла иногда недостаточно.

## 2. Defender.SimpleMongoMigrator

Назначение:

- копировать Mongo-базы между окружениями `local`, `dev`, `prod`;
- переносить данные по префиксам баз:
  - `local_`
  - `dev_`
  - `prod_`

Точки входа:

- основной интерактивный мигратор:
  - `tools/Defender.SimpleMongoMigrator/src/Defender.SimpleMongoMigrator/`
- ручные миграции:
  - `tools/Defender.SimpleMongoMigrator/ManualMigrator/`

Основная команда:

```powershell
dotnet run --project tools/Defender.SimpleMongoMigrator/src/Defender.SimpleMongoMigrator/Defender.SimpleMongoMigrator.csproj
```

Если локально нет нужного runtime, можно собрать self-contained:

```powershell
dotnet publish tools/Defender.SimpleMongoMigrator/src/Defender.SimpleMongoMigrator/Defender.SimpleMongoMigrator.csproj -c Release -r win-x64 --self-contained true -o tools/Defender.SimpleMongoMigrator/output
```

И запускать из папки `output`:

```powershell
.\Defender.SimpleMongoMigrator.exe
```

Поддерживаемая интерактивная команда:

- `MD` — migrate data

После `MD` мигратор спрашивает:

1. `Source env` — откуда читать
2. `Destination env` — куда писать
3. `Target env prefix` — какие базы искать по префиксу
4. `Result env prefix` — с каким префиксом создавать результат

Пример: перенести production secrets в local Mongo:

- `MD`
- `prod`
- `local`
- `prod`
- `local`
- `OK`

Что это делает:

- читает базы с префиксом `prod_`;
- копирует их в `local_`;
- если целевая коллекция уже существует, очищает её перед вставкой.

Важно:

- это не точечная миграция одной коллекции по умолчанию;
- если `DatabaseToMigrate` пустой, мигратор переносит все базы выбранного префикса;
- в нашем случае он перенёс не только `prod_Defender_SecretManagementService`, но и другие `prod_*` базы тоже;
- после завершения при перенаправленном вводе возможен `Console.ReadKey()` exception уже после успешной миграции — это не отменяет сам факт переноса данных.

## 3. Практический локальный workflow для секретов

Когда локально не стартуют `Portal` или `UserManagementService` с ошибкой про пустой `SymmetricSecurityKey`:

1. Проверить, есть ли `Defender_App_JwtSecret` в:
   - `secrets/secrets.local.list`
   - или `tools/Defender.SecretManagementService/secrets/secrets.local.list`
2. Поднять Mongo.
3. Поднять `Defender.SecretManagementService`.
4. Проверить:

```powershell
Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:47056/api/secret/getValue?secretName=JwtSecret"
```

5. Если ответ пустой — прогнать `Defender.SimpleMongoMigrator` и подтянуть `prod_Defender_SecretManagementService` в local Mongo.
6. Перезапустить целевые контейнеры:

```powershell
docker restart LocalPortal LocalUserManagementService
```

7. Проверить health:

```powershell
Invoke-WebRequest -UseBasicParsing http://127.0.0.1:47053/api/home/health
Invoke-WebRequest -UseBasicParsing http://127.0.0.1:47051/api/home/health
```

## 4. Практический локальный workflow для travel-calendar

Минимальный набор:

- `mongo_local`
- `local-travel-calendar-service`
- при Portal smoke также:
  - `LocalPortal`
  - `LocalUserManagementService`
  - `LocalSecretManagementService`

Типовые проблемы:

- `Portal` не открывает `travelCalendar`:
  - проверить, что `LocalTravelCalendarService` реально поднят;
  - проверить `http://127.0.0.1:47064/health`;
  - если `Portal` пишет `Connection refused (host.docker.internal:47064)`, значит travel-calendar не поднят или упал.

- `TravelCalendarService` падает на старых документах:
  - причина: в локальной базе остались старые `TravelCalendars` документы с полем `Events`;
  - решение для локального smoke:

```powershell
docker exec mongo_local mongosh --quiet --eval "db.getSiblingDB('local_Defender_TravelCalendarService').dropDatabase()"
docker restart LocalTravelCalendarService
```

После этого можно снова проверять:

```powershell
Invoke-WebRequest -UseBasicParsing http://127.0.0.1:47064/health
Invoke-WebRequest -UseBasicParsing http://127.0.0.1:47053/api/home/health
```

## 5. Что помнить

- `SecretManagementService` — это инструмент для работы с секретами, а не гарантия, что секреты уже загружены в Mongo.
- `SimpleMongoMigrator` — мощный, но грубый инструмент: он копирует базы по префиксу, а не “аккуратно один ключ”.
- Для локального smoke всегда лучше сначала проверить:
  - health endpoint;
  - наличие секрета;
  - доступность Mongo;
  - только потом уже BFF/API сценарии.
