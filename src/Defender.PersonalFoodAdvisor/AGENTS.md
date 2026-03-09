# AGENTS Guide: Defender.PersonalFoodAdvisor

## Purpose
- Handles Personal Food Advisor workflows: user preferences, menu image parsing, menu confirmation, dish ratings, and recommendation generation.

## Project entry points
- Solution: `Defender.PersonalFoodAdvisor.sln`
- Startup project: `src/WebApi/WebApi.csproj`
- App entry: `src/WebApi/Program.cs`

## Where to change code
- API endpoints: `src/WebApi/Controllers/V1/`
  - `MenuSessionController`, `PreferencesController`, `RatingController`
- Core application services: `src/Application/Services/`
  - `MenuSessionService`, `ImageUploadService`, `MenuParsingProcessor`, `RecommendationProcessor`, `PreferencesService`, `RatingService`
- Kafka + outbox background workers: `src/Application/Services/Background/Kafka/`
  - `FoodAdvisorEventListenerService`, `MenuParsingOutboxPublisherService`, `RecommendationsOutboxPublisherService`, `CreateKafkaTopicsService`
- Data access (Mongo): `src/Infrastructure/Repositories/`
  - `MenuSessionRepository`, `ImageBlobRepository`, `DishRatingRepository`, `UserPreferencesRepository`
  - `MenuParsingOutboxRepository`, `RecommendationsOutboxRepository`, `GeminiModelLoopStateRepository`
- AI/model clients: `src/Infrastructure/Clients/`
  - `Gemini/`, `HuggingFace/`, `MenuIntelligenceClientHelper.cs`

## Main controllers
- `MenuSessionController`
- `PreferencesController`
- `RatingController`
- `HomeController`

## Dependencies and data
- Uses MongoDB for sessions, preferences, image blobs, ratings, outbox messages, and Gemini loop state.
- Uses Kafka topics from `src/Application/Kafka/KafkaTopic.cs`.
- Uses outbox-based publish flow for parsing and recommendations.
- Uses Gemini/HuggingFace clients with model fallback loop logic in Gemini services.

## Portal UI touchpoints
- BFF endpoints live in `src/Defender.Portal/src/WebUI/Controllers/V1/FoodAdvisorController.cs`.
- Client pages live in `src/Defender.Portal/src/WebUI/ClientApp/src/content/appPages/FoodAdvisor/`.
- For Food Advisor UI changes in React, use Material UI (`@mui/material`, `@mui/icons-material`) as the default component library.

## Fast task playbook
- Parsing reliability changes:
  - Start at `MenuSessionService.RequestParsingAsync` (enqueue).
  - Then `MenuParsingOutboxPublisherService` (publish).
  - Then `FoodAdvisorEventListenerService` + `MenuParsingProcessor` (consume/process).
- Recommendation reliability and retries:
  - `RecommendationsOutboxService` and `RecommendationsOutboxPublisherService`.
  - `RecommendationProcessor` for retry scheduling and warning behavior.
- Model fallback/rate-limit behavior:
  - `Infrastructure/Clients/Gemini/GeminiModelFallbackService.cs`
  - `Infrastructure/Clients/Gemini/GeminiModelLoopMaintenanceService.cs`
  - `src/WebApi/appsettings*.json` Gemini options.
- Session delete side effects:
  - Validate cleanup in `MenuSessionService.DeleteAsync` for blobs, ratings, and both outboxes.

## Commands
- Run: `dotnet run --project src/WebApi/WebApi.csproj`
- Build: `dotnet build Defender.PersonalFoodAdvisor.sln`
- Test: `dotnet test Defender.PersonalFoodAdvisor.sln`
- Test project only: `dotnet test src/Tests/Defender.PersonalFoodAdvisor.Tests.csproj`
