# AGENTS Guide: Defender.Kafka

## Purpose
- Shared Kafka integration library for producers/consumers and background services.

## Project entry points
- Solution: `Defender.Kafka.sln`
- Library project: `src/Defender.Kafka/Defender.Kafka.csproj`

## Where to change code
- Core service abstractions: `src/Defender.Kafka/Service/`
- Background workers: `src/Defender.Kafka/BackgroundServices/`
- Message defaults/contracts: `src/Defender.Kafka/Default/`, `CorrelatedMessage/`
- Serialization: `src/Defender.Kafka/Serialization/`
- Configuration/DI: `src/Defender.Kafka/Configuration/`, `Extension/`

## Fast task playbook
- Add/adjust consumer behavior: update service/background-service components.
- Add config option support: update `Configuration/` plus extension wiring.
- Change serialization: update `Serialization/` and verify compatibility for consuming services.

## Commands
- Build: `dotnet build Defender.Kafka.sln`
- Test (if added): `dotnet test Defender.Kafka.sln`
