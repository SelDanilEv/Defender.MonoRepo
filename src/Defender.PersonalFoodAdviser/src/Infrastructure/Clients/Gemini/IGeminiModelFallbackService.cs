using Defender.PersonalFoodAdviser.Domain.Enums;

namespace Defender.PersonalFoodAdviser.Infrastructure.Clients.Gemini;

public interface IGeminiModelFallbackService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task ResetLoopsAsync(CancellationToken cancellationToken = default);

    Task<TResult> ExecuteAsync<TResult>(
        GeminiModelRoute route,
        Func<string, CancellationToken, Task<TResult>> executeForModel,
        CancellationToken cancellationToken = default);
}
