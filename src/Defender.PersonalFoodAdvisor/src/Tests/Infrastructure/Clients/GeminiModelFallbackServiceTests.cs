using System.Net;
using Defender.PersonalFoodAdvisor.Application.Common.Interfaces.Repositories;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Defender.PersonalFoodAdvisor.Domain.Enums;
using Defender.PersonalFoodAdvisor.Infrastructure.Clients.Gemini;
using Defender.PersonalFoodAdvisor.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Defender.PersonalFoodAdvisor.Tests.Infrastructure.Clients;

public class GeminiModelFallbackServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPrimaryModelReturnsTooManyRequests_SwitchesActiveModelForNextRequest()
    {
        var timeProvider = new FakeTimeProvider();
        var repository = new FakeGeminiModelLoopStateRepository();
        var sut = CreateSut(repository, timeProvider, opts => opts.RecommendationFallbackModels = ["gemini-2.5-flash"]);
        var modelsUsed = new List<string>();

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) =>
            {
                modelsUsed.Add(model);
                throw new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests);
            }));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);

        var result = await sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) =>
            {
                modelsUsed.Add(model);
                return Task.FromResult("ok");
            });

        Assert.Equal("ok", result);
        Assert.Equal(["gemini-2.5-flash-lite", "gemini-2.5-flash"], modelsUsed);
        Assert.Contains(
            repository.Upserts,
            state => state.Route == GeminiModelRoute.Recommendation
                && state.ActiveModelIndex == 1
                && state.Models.SequenceEqual(["gemini-2.5-flash-lite", "gemini-2.5-flash"]));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPrimaryModelReturnsTooManyRequests_RethrowsRateLimitWithoutImmediateRetry()
    {
        var timeProvider = new FakeTimeProvider();
        var sut = CreateSut(
            new FakeGeminiModelLoopStateRepository(),
            timeProvider,
            opts => opts.RecommendationFallbackModels = ["gemini-2.5-flash"]);
        var modelsUsed = new List<string>();

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) =>
            {
                modelsUsed.Add(model);
                throw new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests);
            }));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.Equal(["gemini-2.5-flash-lite"], modelsUsed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRateLimitHappensAgainBeforeCooldown_DoesNotSwitchBackImmediately()
    {
        var timeProvider = new FakeTimeProvider();
        var sut = CreateSut(
            new FakeGeminiModelLoopStateRepository(),
            timeProvider,
            opts => opts.RecommendationFallbackModels = ["gemini-2.5-flash"]);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) => model == "gemini-2.5-flash-lite"
                ? throw new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests)
                : Task.FromResult("ok")));

        var modelsUsed = new List<string>();

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) =>
            {
                modelsUsed.Add(model);
                throw new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests);
            }));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.Equal(["gemini-2.5-flash"], modelsUsed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCooldownElapsed_WrapsBackToFirstModel()
    {
        var timeProvider = new FakeTimeProvider();
        var sut = CreateSut(
            new FakeGeminiModelLoopStateRepository(),
            timeProvider,
            opts => opts.RecommendationFallbackModels = ["gemini-2.5-flash"]);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) => model == "gemini-2.5-flash-lite"
                ? throw new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests)
                : Task.FromResult("ok")));

        timeProvider.Advance(TimeSpan.FromSeconds(46));

        var modelsUsed = new List<string>();

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) =>
            {
                modelsUsed.Add(model);
                throw new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests);
            }));

        var result = await sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) =>
            {
                modelsUsed.Add(model);
                return Task.FromResult("ok");
            });

        Assert.Equal("ok", result);
        Assert.Equal(["gemini-2.5-flash", "gemini-2.5-flash-lite"], modelsUsed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVisionRouteRateLimited_DoesNotChangeRecommendationRoute()
    {
        var timeProvider = new FakeTimeProvider();
        var sut = CreateSut(new FakeGeminiModelLoopStateRepository(), timeProvider);
        var recommendationModels = new List<string>();

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.ExecuteAsync<string>(
            GeminiModelRoute.Vision,
            (model, _) => throw new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests)));

        var result = await sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) =>
            {
                recommendationModels.Add(model);
                return Task.FromResult("ok");
            });

        Assert.Equal("ok", result);
        Assert.Equal(["gemini-2.5-flash-lite"], recommendationModels);
    }

    [Fact]
    public async Task InitializeAsync_WhenPersistedStateExists_UsesPersistedActiveModel()
    {
        var timeProvider = new FakeTimeProvider();
        var repository = new FakeGeminiModelLoopStateRepository(
            new GeminiModelLoopState
            {
                Id = GeminiModelRoute.Recommendation.ToString(),
                Route = GeminiModelRoute.Recommendation,
                Models = ["gemini-3-flash-preview", "gemini-2.5-flash"],
                ActiveModelIndex = 1,
                LastSwitchAtUtc = new DateTime(2026, 3, 4, 11, 30, 0, DateTimeKind.Utc),
                LastResetDateUtc = new DateTime(2026, 3, 4, 0, 0, 0, DateTimeKind.Utc)
            });
        var sut = CreateSut(repository, timeProvider);
        var usedModels = new List<string>();

        var result = await sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) =>
            {
                usedModels.Add(model);
                return Task.FromResult("ok");
            });

        Assert.Equal("ok", result);
        Assert.Equal(["gemini-2.5-flash"], usedModels);
    }

    [Fact]
    public async Task InitializeAsync_WhenPersistedStateIsFromPreviousUtcDay_ResetsToConfiguredPrimaryModel()
    {
        var timeProvider = new FakeTimeProvider();
        var repository = new FakeGeminiModelLoopStateRepository(
            new GeminiModelLoopState
            {
                Id = GeminiModelRoute.Recommendation.ToString(),
                Route = GeminiModelRoute.Recommendation,
                Models = ["gemini-3-flash-preview", "gemini-2.5-flash"],
                ActiveModelIndex = 1,
                LastSwitchAtUtc = new DateTime(2026, 3, 3, 23, 30, 0, DateTimeKind.Utc),
                LastResetDateUtc = new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc)
            });
        var sut = CreateSut(repository, timeProvider);
        var usedModels = new List<string>();

        var result = await sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) =>
            {
                usedModels.Add(model);
                return Task.FromResult("ok");
            });

        Assert.Equal("ok", result);
        Assert.Equal(["gemini-2.5-flash-lite"], usedModels);
        Assert.Contains(
            repository.Upserts,
            state => state.Route == GeminiModelRoute.Recommendation
                && state.ActiveModelIndex == 0
                && state.Models.SequenceEqual(["gemini-2.5-flash-lite", "gemini-3-flash-preview", "gemini-2.5-flash", "gemma-3-4b-it"]));
    }

    [Fact]
    public async Task ResetLoopsAsync_WhenCalledAtMidnight_ResetsEachRouteToConfiguredPrimaryModel()
    {
        var timeProvider = new FakeTimeProvider();
        var repository = new FakeGeminiModelLoopStateRepository(
            new GeminiModelLoopState
            {
                Id = GeminiModelRoute.Vision.ToString(),
                Route = GeminiModelRoute.Vision,
                Models = ["gemini-3-flash-preview", "gemini-2.5-flash-lite"],
                ActiveModelIndex = 1,
                LastSwitchAtUtc = new DateTime(2026, 3, 4, 23, 40, 0, DateTimeKind.Utc),
                LastResetDateUtc = new DateTime(2026, 3, 4, 0, 0, 0, DateTimeKind.Utc)
            },
            new GeminiModelLoopState
            {
                Id = GeminiModelRoute.Recommendation.ToString(),
                Route = GeminiModelRoute.Recommendation,
                Models = ["gemini-2.5-flash", "gemma-3-4b-it"],
                ActiveModelIndex = 1,
                LastSwitchAtUtc = new DateTime(2026, 3, 4, 23, 40, 0, DateTimeKind.Utc),
                LastResetDateUtc = new DateTime(2026, 3, 4, 0, 0, 0, DateTimeKind.Utc)
            });
        var sut = CreateSut(repository, timeProvider);

        await sut.InitializeAsync();
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero));

        await sut.ResetLoopsAsync();

        var visionResult = await sut.ExecuteAsync<string>(
            GeminiModelRoute.Vision,
            (model, _) => Task.FromResult(model));
        var recommendationResult = await sut.ExecuteAsync<string>(
            GeminiModelRoute.Recommendation,
            (model, _) => Task.FromResult(model));

        Assert.Equal("gemini-2.5-flash", visionResult);
        Assert.Equal("gemini-2.5-flash-lite", recommendationResult);
    }

    private static GeminiModelFallbackService CreateSut(
        IGeminiModelLoopStateRepository repository,
        TimeProvider timeProvider,
        Action<GeminiOptions>? configureOptions = null)
    {
        var options = new GeminiOptions
        {
            VisionModel = "gemini-2.5-flash",
            VisionFallbackModels = ["gemini-3-flash-preview", "gemini-2.5-flash-lite"],
            RecommendationModel = "gemini-2.5-flash-lite",
            RecommendationFallbackModels = ["gemini-3-flash-preview", "gemini-2.5-flash", "gemma-3-4b-it"],
            ModelSwitchCooldownSeconds = 45
        };
        configureOptions?.Invoke(options);
        return new GeminiModelFallbackService(
            repository,
            Options.Create(options),
            timeProvider,
            new Mock<ILogger<GeminiModelFallbackService>>().Object);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
        public void SetUtcNow(DateTimeOffset value) => _utcNow = value;
    }

    private sealed class FakeGeminiModelLoopStateRepository(params GeminiModelLoopState[] seededStates) : IGeminiModelLoopStateRepository
    {
        private readonly Dictionary<GeminiModelRoute, GeminiModelLoopState> _states = seededStates.ToDictionary(x => x.Route);

        public List<GeminiModelLoopState> Upserts { get; } = [];

        public Task<IReadOnlyList<GeminiModelLoopState>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<GeminiModelLoopState>>(_states.Values.Select(Clone).ToList());
        }

        public Task UpsertAsync(GeminiModelLoopState state, CancellationToken cancellationToken = default)
        {
            var clone = Clone(state);
            _states[clone.Route] = clone;
            Upserts.Add(clone);
            return Task.CompletedTask;
        }

        private static GeminiModelLoopState Clone(GeminiModelLoopState state)
        {
            return new GeminiModelLoopState
            {
                Id = state.Id,
                Route = state.Route,
                Models = state.Models.ToList(),
                ActiveModelIndex = state.ActiveModelIndex,
                LastSwitchAtUtc = state.LastSwitchAtUtc,
                LastResetDateUtc = state.LastResetDateUtc,
                CreatedAtUtc = state.CreatedAtUtc,
                UpdatedAtUtc = state.UpdatedAtUtc
            };
        }
    }
}
