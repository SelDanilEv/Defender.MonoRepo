using System.Net;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Defender.PersonalFoodAdvisor.Domain.Enums;
using Defender.PersonalFoodAdvisor.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdvisor.Infrastructure.Clients.Gemini;

public class GeminiModelFallbackService(
    IGeminiModelLoopStateRepository repository,
    IOptions<GeminiOptions> options,
    TimeProvider timeProvider,
    ILogger<GeminiModelFallbackService> logger) : IGeminiModelFallbackService
{
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly TimeSpan _switchCooldown = TimeSpan.FromSeconds(Math.Max(1, options.Value.ModelSwitchCooldownSeconds));
    private readonly IReadOnlyDictionary<GeminiModelRoute, IReadOnlyList<string>> _configuredRouteModels = BuildConfiguredRouteModels(options.Value);
    private Dictionary<GeminiModelRoute, RouteState> _routeStates = [];
    private bool _initialized;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken);

        try
        {
            if (_initialized)
                return;

            var todayUtc = GetCurrentUtcDate();
            var persistedStates = (await repository.GetAllAsync(cancellationToken))
                .ToDictionary(x => x.Route);
            var statesToPersist = new List<RouteState>();

            _routeStates = new Dictionary<GeminiModelRoute, RouteState>();

            foreach (var (route, configuredModels) in _configuredRouteModels)
            {
                var state = CreateRouteState(route, configuredModels, persistedStates.GetValueOrDefault(route), todayUtc, statesToPersist);
                _routeStates[route] = state;
            }

            _initialized = true;

            foreach (var state in statesToPersist)
            {
                await repository.UpsertAsync(ToDocument(state), cancellationToken);
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task ResetLoopsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await ResetLoopsCoreAsync(force: true, cancellationToken);
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        GeminiModelRoute route,
        Func<string, CancellationToken, Task<TResult>> executeForModel,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await ResetLoopsCoreAsync(force: false, cancellationToken);

        var model = await GetActiveModelAsync(route, cancellationToken);

        try
        {
            return await executeForModel(model, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            await HandleRateLimitAsync(route, model, ex, cancellationToken);
            throw;
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        await InitializeAsync(cancellationToken);
    }

    private async Task<string> GetActiveModelAsync(GeminiModelRoute route, CancellationToken cancellationToken)
    {
        await _stateLock.WaitAsync(cancellationToken);

        try
        {
            var state = _routeStates[route];
            return state.Models[state.ActiveModelIndex];
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task HandleRateLimitAsync(
        GeminiModelRoute route,
        string attemptedModel,
        HttpRequestException ex,
        CancellationToken cancellationToken)
    {
        GeminiModelLoopState? stateToPersist = null;

        await _stateLock.WaitAsync(cancellationToken);

        try
        {
            var state = _routeStates[route];
            if (state.Models.Count <= 1)
            {
                logger.LogWarning(ex, "Gemini route {Route} model {Model} returned 429 and no fallback models are configured", route, attemptedModel);
                return;
            }

            var activeModel = state.Models[state.ActiveModelIndex];
            if (!string.Equals(activeModel, attemptedModel, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    ex,
                    "Gemini route {Route} model {AttemptedModel} returned 429. Active model is already {ActiveModel}; the next request will use the active model",
                    route,
                    attemptedModel,
                    activeModel);
                return;
            }

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            if (state.LastSwitchAtUtc.HasValue && nowUtc - state.LastSwitchAtUtc.Value < _switchCooldown)
            {
                logger.LogWarning(
                    ex,
                    "Gemini route {Route} model {Model} returned 429 but model switching is throttled for {CooldownSeconds} seconds",
                    route,
                    attemptedModel,
                    _switchCooldown.TotalSeconds);
                return;
            }

            state.ActiveModelIndex = (state.ActiveModelIndex + 1) % state.Models.Count;
            state.LastSwitchAtUtc = nowUtc;

            var fallbackModel = state.Models[state.ActiveModelIndex];
            stateToPersist = ToDocument(state);
            logger.LogWarning(
                ex,
                "Gemini route {Route} model {Model} returned 429. Switching active model to {FallbackModel} for subsequent requests",
                route,
                attemptedModel,
                fallbackModel);
        }
        finally
        {
            _stateLock.Release();
        }

        if (stateToPersist != null)
            await repository.UpsertAsync(stateToPersist, cancellationToken);
    }

    private async Task ResetLoopsCoreAsync(bool force, CancellationToken cancellationToken)
    {
        var todayUtc = GetCurrentUtcDate();
        List<GeminiModelLoopState>? statesToPersist = null;

        await _stateLock.WaitAsync(cancellationToken);

        try
        {
            statesToPersist = [];

            foreach (var (route, state) in _routeStates)
            {
                if (!force && state.LastResetDateUtc >= todayUtc)
                    continue;

                state.Models = _configuredRouteModels[route].ToList();
                state.ActiveModelIndex = 0;
                state.LastSwitchAtUtc = null;
                state.LastResetDateUtc = todayUtc;
                statesToPersist.Add(ToDocument(state));
            }
        }
        finally
        {
            _stateLock.Release();
        }

        foreach (var state in statesToPersist)
        {
            await repository.UpsertAsync(state, cancellationToken);
        }
    }

    private static IReadOnlyDictionary<GeminiModelRoute, IReadOnlyList<string>> BuildConfiguredRouteModels(GeminiOptions options)
    {
        return new Dictionary<GeminiModelRoute, IReadOnlyList<string>>
        {
            [GeminiModelRoute.Vision] = GetConfiguredModels(options.VisionModel, options.VisionFallbackModels),
            [GeminiModelRoute.Recommendation] = GetConfiguredModels(options.RecommendationModel, options.RecommendationFallbackModels)
        };
    }

    private RouteState CreateRouteState(
        GeminiModelRoute route,
        IReadOnlyList<string> configuredModels,
        GeminiModelLoopState? persistedState,
        DateTime todayUtc,
        ICollection<RouteState> statesToPersist)
    {
        if (persistedState == null || persistedState.Models.Count == 0 || persistedState.LastResetDateUtc.Date < todayUtc)
        {
            var resetState = CreateDefaultState(route, configuredModels, todayUtc);
            statesToPersist.Add(resetState);
            return resetState;
        }

        var models = persistedState.Models
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (models.Count == 0)
        {
            var resetState = CreateDefaultState(route, configuredModels, todayUtc);
            statesToPersist.Add(resetState);
            return resetState;
        }

        var activeModelIndex = persistedState.ActiveModelIndex;
        if (activeModelIndex < 0 || activeModelIndex >= models.Count)
        {
            activeModelIndex = 0;
            statesToPersist.Add(new RouteState(route, models, activeModelIndex, persistedState.LastSwitchAtUtc, persistedState.LastResetDateUtc));
        }

        return new RouteState(route, models, activeModelIndex, persistedState.LastSwitchAtUtc, persistedState.LastResetDateUtc);
    }

    private static RouteState CreateDefaultState(GeminiModelRoute route, IReadOnlyList<string> configuredModels, DateTime todayUtc)
    {
        return new RouteState(route, configuredModels.ToList(), 0, null, todayUtc);
    }

    private GeminiModelLoopState ToDocument(RouteState state)
    {
        return new GeminiModelLoopState
        {
            Id = state.Route.ToString(),
            Route = state.Route,
            Models = state.Models.ToList(),
            ActiveModelIndex = state.ActiveModelIndex,
            LastSwitchAtUtc = state.LastSwitchAtUtc,
            LastResetDateUtc = state.LastResetDateUtc,
            UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };
    }

    private DateTime GetCurrentUtcDate()
    {
        return timeProvider.GetUtcNow().UtcDateTime.Date;
    }

    private static IReadOnlyList<string> GetConfiguredModels(string primaryModel, IReadOnlyList<string> fallbackModels)
    {
        var configured = new List<string>(1 + fallbackModels.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddModel(configured, seen, primaryModel);

        foreach (var fallbackModel in fallbackModels)
        {
            AddModel(configured, seen, fallbackModel);
        }

        if (configured.Count == 0)
            throw new InvalidOperationException("At least one Gemini model must be configured.");

        return configured;
    }

    private static void AddModel(ICollection<string> target, ISet<string> seen, string? model)
    {
        var value = model?.Trim();
        if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
            return;

        target.Add(value);
    }

    private sealed class RouteState(
        GeminiModelRoute route,
        IReadOnlyList<string> models,
        int activeModelIndex,
        DateTime? lastSwitchAtUtc,
        DateTime lastResetDateUtc)
    {
        public GeminiModelRoute Route { get; } = route;
        public List<string> Models { get; set; } = models.ToList();
        public int ActiveModelIndex { get; set; } = activeModelIndex;
        public DateTime? LastSwitchAtUtc { get; set; } = lastSwitchAtUtc;
        public DateTime LastResetDateUtc { get; set; } = lastResetDateUtc;
    }
}
