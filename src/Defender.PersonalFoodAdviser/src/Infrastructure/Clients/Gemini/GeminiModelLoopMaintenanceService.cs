using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Defender.PersonalFoodAdviser.Infrastructure.Clients.Gemini;

public class GeminiModelLoopMaintenanceService(
    IGeminiModelFallbackService modelFallbackService,
    TimeProvider timeProvider,
    ILogger<GeminiModelLoopMaintenanceService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await modelFallbackService.InitializeAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = timeProvider.GetUtcNow();
            var nextResetUtc = new DateTimeOffset(nowUtc.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
            var delay = nextResetUtc - nowUtc;

            logger.LogInformation("Gemini model loop maintenance active. Next UTC reset at {NextResetUtc}", nextResetUtc);

            await Task.Delay(delay, stoppingToken);

            logger.LogInformation("Resetting Gemini model loops at UTC midnight");
            await modelFallbackService.ResetLoopsAsync(stoppingToken);
        }
    }
}
