# Defender Travel Calendar Service

Owns each authenticated user's travel calendar shell, scheduled and queued trips, shared-event participants, POIs, budgets, theme, and packing checklist. Personal settings are stored in MongoDB collection `TravelCalendars`, while canonical shared events are stored in `TravelEvents`.

## Local development

```powershell
dotnet restore Defender.TravelCalendarService.sln
dotnet test Defender.TravelCalendarService.sln
dotnet run --project src/WebApi/WebApi.csproj --launch-profile Local
```

The API is served on `http://localhost:47064`; health is `GET /health`; authenticated operations are rooted at `/api/V1/travel-calendar`.

Local and Debug profiles use a documented development-only JWT key when the local secret store has not been seeded. Dev and Prod always require `JwtSecret` from the normal secret provider.
