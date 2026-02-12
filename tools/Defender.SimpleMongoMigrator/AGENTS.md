# AGENTS Guide: Defender.SimpleMongoMigrator

## Purpose
- Provides MongoDB migration tooling between environments.

## Project entry points
- Solution: `Defender.SimpleMongoMigrator.sln`
- Main console app: `src/Defender.SimpleMongoMigrator/Defender.SimpleMongoMigrator.csproj`
- Additional console app: `ManualMigrator/ManualMigrator.csproj`

## Where to change code
- Interactive migration flow: `src/Defender.SimpleMongoMigrator/Program.cs`
- Migration engine: `src/Defender.SimpleMongoMigrator/MongoMigrator.cs`
- Manual migration scripts: `ManualMigrator/`
- Shared environment helpers: `MigratorLib/Settings/Env.cs`

## Runtime behavior
- Main app uses command-driven flow (`MD`) to migrate selected environments.
- Requires connection strings in appsettings (`LocalMongoDBConnectionString`, `DevMongoDBConnectionString`, `CloudMongoDBConnectionString`).

## Fast task playbook
- Add migration step: implement in `MongoMigrator.cs` and/or `ManualMigrator/` classes.
- Extend environment mapping: update `MigratorLib/Settings/Env.cs`.
- Change CLI UX: update prompt/flow in `Program.cs`.

## Commands
- Run main migrator: `dotnet run --project src/Defender.SimpleMongoMigrator/Defender.SimpleMongoMigrator.csproj`
- Run manual migrator: `dotnet run --project ManualMigrator/ManualMigrator.csproj`
- Build: `dotnet build Defender.SimpleMongoMigrator.sln`
