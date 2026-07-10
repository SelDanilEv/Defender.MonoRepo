# AGENTS Guide: Defender.TravelCalendarService

User-scoped Q3 2026 travel calendar. Domain invariants live in `src/Domain`, orchestration in `src/Application`, Mongo persistence in `src/Infrastructure`, and HTTP endpoints in `src/WebApi`.

- Build: `dotnet build Defender.TravelCalendarService.sln`
- Test: `dotnet test Defender.TravelCalendarService.sln`
- Run: `dotnet run --project src/WebApi/WebApi.csproj --launch-profile Local`
- Local port: `47064`
- Mongo: `local_Defender_TravelCalendarService.TravelCalendars`
