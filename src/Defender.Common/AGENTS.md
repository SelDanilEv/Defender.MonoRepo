# AGENTS Guide: Defender.Common

## Purpose
- Shared .NET library used across Defender services.
- Contains common models, helpers, and generated clients.

## Project entry points
- Solution: `Defender.Common.sln`
- Library project: `src/Defender.Common/Defender.Common.csproj`

## Where to change code
- Shared primitives/utilities: `src/Defender.Common/`
- Generated or service-specific client contracts: `src/Defender.Common/NSwag/`

## Dependencies
- Referenced by multiple services; treat changes as cross-service impact.

## Fast task playbook
- Add shared DTO/helper: update `src/Defender.Common/` and validate all dependent services compile.
- Update client contract shape: change NSwag/client-related code and validate usage sites.

## Commands
- Build: `dotnet build Defender.Common.sln`
- Test (if added): `dotnet test Defender.Common.sln`
