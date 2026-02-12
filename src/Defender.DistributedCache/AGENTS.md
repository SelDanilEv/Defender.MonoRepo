# AGENTS Guide: Defender.DistributedCache

## Purpose
- Shared distributed cache implementation backed by PostgreSQL.

## Project entry points
- Solution: `Defender.DistributedCache.sln`
- Library project: `src/Defender.DistributedCache/Defender.DistributedCache.csproj`
- Test host API: `src/Defender.DistributedCacheTestWebApi/Defender.DistributedCacheTestWebApi.csproj`

## Where to change code
- Cache implementation: `src/Defender.DistributedCache/Postgres/`
- Config/options: `src/Defender.DistributedCache/Configuration/`
- Hosted cleanup behavior: `Postgres/PostgresCacheCleanupService*`
- Test API endpoint: `src/Defender.DistributedCacheTestWebApi/Controllers/`

## Dependencies and data
- Uses Npgsql/PostgreSQL.
- Consumed by services that need shared cache semantics.

## Fast task playbook
- Change cache storage/query behavior: update `PostgresDistributedCache*` classes.
- Add config knobs: update options in `Configuration/` and DI setup.
- Validate behavior with test web API and unit tests.

## Commands
- Run test host: `dotnet run --project src/Defender.DistributedCacheTestWebApi/Defender.DistributedCacheTestWebApi.csproj`
- Build: `dotnet build Defender.DistributedCache.sln`
- Test: `dotnet test Defender.DistributedCache.sln`
