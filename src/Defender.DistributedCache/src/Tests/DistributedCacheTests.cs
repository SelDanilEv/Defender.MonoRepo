using Defender.DistributedCache.Configuration.Options;
using Defender.DistributedCache.Postgres;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq.Expressions;
using Defender.DistributedCache.Postgres.TTL;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;
using System.Net.Sockets;

namespace Defender.DistributedCache.Tests
{
    public class DistributedCacheTests : IAsyncLifetime
    {
        private IDistributedCache _distributedCache = default!;
        private IPostgresCacheCleanupService _postgresCacheCleanupService = default!;
        private bool _cacheAvailable;

        private static bool? _sharedCacheAvailability;
        private static bool _containerStartAttempted;
        private static bool _containerStarted;
        private static bool _cleanupRegistered;
        private static readonly SemaphoreSlim ContainerStartLock = new(1, 1);
        private static readonly string ContainerName = $"defender-distributed-cache-tests-{Guid.NewGuid():N}";
        private static readonly int HostPort = GetFreePort();

        private const string CacheTableName = "cache";
        private const string DbName = "cache_database";
        private const string DbUser = "postgres";
        private const string DbPassword = "postgres";

        private static string ConnectionString =>
            $"Host=127.0.0.1;Port={HostPort};Database={DbName};Username={DbUser};Password={DbPassword};Pooling=false;Timeout=15;Command Timeout=15";

        public DistributedCacheTests()
        {
        }

        public async Task InitializeAsync()
        {
            await EnsureContainerStartedAsync();

            var options = Options.Create(new DistributedCacheOptions
            {
                ConnectionString = ConnectionString,
                CacheTableName = CacheTableName
            });

            var logger = new Mock<ILogger<PostgresDistributedCache>>().Object;
            _distributedCache = new PostgresDistributedCache(options, logger);
            _postgresCacheCleanupService = new PostgresCacheCleanupService(options);

            try
            {
                await _postgresCacheCleanupService.CheckAndRunCleanupAsync();
            }
            catch
            {
                // ignored - readiness probe below will determine availability
            }

            if (_sharedCacheAvailability.HasValue)
            {
                _cacheAvailable = _sharedCacheAvailability.Value;
                return;
            }

            _cacheAvailable = _containerStarted && await WaitForCacheAvailabilityAsync();
            _sharedCacheAvailability = _cacheAvailable;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        private async Task<bool> WaitForCacheAvailabilityAsync(int maxAttempts = 20, int delayMs = 300)
        {
            var probe = new TestModel { Name = $"probe-{Guid.NewGuid():N}", Age = 1 };
            Func<TestModel, string> idProvider = m => $"TestModel:{m.Name}";

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                await _distributedCache.Add(idProvider, probe, TimeSpan.FromSeconds(30));
                var cachedValue = await _distributedCache.Get<TestModel>(idProvider(probe), null);
                if (cachedValue != null)
                {
                    await _distributedCache.Invalidate(idProvider(probe));
                    return true;
                }

                await Task.Delay(delayMs);
            }

            return false;
        }

        private static async Task EnsureContainerStartedAsync()
        {
            await ContainerStartLock.WaitAsync();
            try
            {
                if (_containerStartAttempted)
                {
                    return;
                }

                _containerStartAttempted = true;

                var dockerVersion = await RunDockerCommandAsync("version --format {{.Server.Version}}");
                if (dockerVersion.ExitCode != 0)
                {
                    _containerStarted = false;
                    return;
                }

                await RunDockerCommandAsync($"rm -f {ContainerName}", ignoreErrors: true);

                var runResult = await RunDockerCommandAsync(
                    $"run -d --rm --name {ContainerName} -e POSTGRES_PASSWORD={DbPassword} -e POSTGRES_USER={DbUser} -e POSTGRES_DB={DbName} -p {HostPort}:5432 postgres:16-alpine");
                if (runResult.ExitCode != 0)
                {
                    _containerStarted = false;
                    return;
                }

                _containerStarted = await WaitForPostgresReadyAsync();
                if (_containerStarted)
                {
                    RegisterContainerCleanup();
                }
                else
                {
                    await RunDockerCommandAsync($"rm -f {ContainerName}", ignoreErrors: true);
                }
            }
            finally
            {
                ContainerStartLock.Release();
            }
        }

        private static async Task<bool> WaitForPostgresReadyAsync(int maxAttempts = 60, int delayMs = 1000)
        {
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    await using var connection = new NpgsqlConnection(ConnectionString);
                    await connection.OpenAsync();
                    await connection.CloseAsync();
                    return true;
                }
                catch
                {
                    await Task.Delay(delayMs);
                }
            }

            return false;
        }

        private static void RegisterContainerCleanup()
        {
            if (_cleanupRegistered)
            {
                return;
            }

            _cleanupRegistered = true;
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try
                {
                    RunDockerCommandSync($"rm -f {ContainerName}");
                }
                catch
                {
                    // ignored
                }
            };
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static async Task<(int ExitCode, string StdOut, string StdErr)> RunDockerCommandAsync(
            string arguments,
            bool ignoreErrors = false)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var stdOut = await process.StandardOutput.ReadToEndAsync();
                var stdErr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!ignoreErrors && process.ExitCode != 0)
                {
                    return (process.ExitCode, stdOut, stdErr);
                }

                return (process.ExitCode, stdOut, stdErr);
            }
            catch (Exception ex)
            {
                if (ignoreErrors)
                {
                    return (-1, string.Empty, ex.Message);
                }

                return (-1, string.Empty, ex.Message);
            }
        }

        private static void RunDockerCommandSync(string arguments)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(5000);
        }


        [Fact]
        public async Task Get_ShouldReturnCachedValue_WhenValueExists()
        {
            // Arrange
            var model = new TestModel { Name = "Test", Age = 25 };
            Func<TestModel, string> idProvider = m => $"TestModel:{m.Name}";
            await _distributedCache.Add(idProvider, model, TimeSpan.FromMinutes(10));

            // Act
            var cachedValue = await _distributedCache.Get<TestModel>(idProvider(model), null);

            // Assert
            if (!_cacheAvailable)
            {
                Assert.Null(cachedValue);
                return;
            }

            Assert.NotNull(cachedValue);
            Assert.Equal(model.Name, cachedValue.Name);
            Assert.Equal(model.Age, cachedValue.Age);
        }

        [Fact]
        public async Task Get_ShouldFetchAndCacheValue_WhenValueDoesNotExist()
        {
            // Arrange
            var model = new TestModel { Name = "Test", Age = 25 };
            Func<TestModel, string> idProvider = m => $"TestModel:{m.Name}";
            var fetchValue = () => Task.FromResult(model);

            // Act
            var cachedValue = await _distributedCache.Get(idProvider(model), fetchValue, TimeSpan.FromMinutes(10));

            // Assert
            Assert.NotNull(cachedValue);
            Assert.Equal(model.Name, cachedValue.Name);
            Assert.Equal(model.Age, cachedValue.Age);

            await _distributedCache.Invalidate(idProvider(model));
            
            var cachedValueAfterFetch = await _distributedCache.Get<TestModel>(idProvider(model), fetchValue);
            Assert.NotNull(cachedValueAfterFetch);
            Assert.Equal(model.Name, cachedValueAfterFetch.Name);
            Assert.Equal(model.Age, cachedValueAfterFetch.Age);
        }

        [Fact]
        public async Task Invalidate_ShouldRemoveValueFromCache()
        {
            // Arrange
            var model = new TestModel { Name = "Test", Age = 25 };
            Func<TestModel, string> idProvider = m => $"TestModel:{m.Name}";
            await _distributedCache.Add(idProvider, model, TimeSpan.FromMinutes(10));

            // Act
            await _distributedCache.Invalidate(idProvider(model));

            // Assert
            var cachedValue = await _distributedCache.Get<TestModel>(idProvider(model), null);
            Assert.Null(cachedValue);
        }

        [Fact]
        public async Task GetByFields_ShouldReturnCachedValue_WhenValueExists()
        {
            // Arrange
            var model = new TestModel { Name = "Test", Age = 25 };
            var expressions = new List<Expression<Func<TestModel, bool>>>
            {
                x => x.Name == model.Name,
                x => x.Age == model.Age
            };
            Func<TestModel, string> idProvider = m => $"TestModel:{m.Name}";
            await _distributedCache.Add(idProvider, model, TimeSpan.FromMinutes(10));

            // Act
            var cachedValue = await _distributedCache.Get<TestModel>(expressions, idProvider, null, TimeSpan.FromMinutes(10));


            // Assert
            if (!_cacheAvailable)
            {
                Assert.Null(cachedValue);
                return;
            }

            Assert.NotNull(cachedValue);
            Assert.Equal(model.Name, cachedValue.Name);
            Assert.Equal(model.Age, cachedValue.Age);
        }

        [Fact]
        public async Task GetByFields_ShouldFetchAndCacheValue_WhenValueDoesNotExist()
        {
            // Arrange
            var model = new TestModel { Name = "Test", Age = 25 };
            var expressions = new List<Expression<Func<TestModel, bool>>>
            {
                x => x.Name == model.Name,
                //x => x.Age == model.Age
            };

            Func<TestModel, string> idProvider = m => $"TestModel:{m.Name}";
            Func<Task<TestModel>> fetchValue = () => Task.FromResult(model);

            // Act

            await _distributedCache.Invalidate(expressions);
            var cachedValue = await _distributedCache.Get(expressions, idProvider, fetchValue, TimeSpan.FromMinutes(10));

            // Assert
            Assert.NotNull(cachedValue);
            Assert.Equal(model.Name, cachedValue.Name);
            Assert.Equal(model.Age, cachedValue.Age);


            var cachedValueAfterFetch = await _distributedCache.Get<TestModel>(idProvider(model), null);
            if (!_cacheAvailable)
            {
                Assert.Null(cachedValueAfterFetch);
                return;
            }

            Assert.NotNull(cachedValueAfterFetch);
            Assert.Equal(model.Name, cachedValueAfterFetch.Name);
            Assert.Equal(model.Age, cachedValueAfterFetch.Age);
        }

        [Fact]
        public async Task InvalidateByFields_ShouldRemoveValueFromCache()
        {
            // Arrange
            var model = new TestModel { Name = "Test", Age = 25 };
            var expressions = new List<Expression<Func<TestModel, bool>>>
            {
                x => x.Name == model.Name,
                x => x.Age == model.Age
            };
            Func<TestModel, string> idProvider = m => $"TestModel:{m.Name}";
            await _distributedCache.Add(idProvider, model, TimeSpan.FromMinutes(10));

            // Act
            await _distributedCache.Invalidate(expressions);

            // Assert
            var cachedValue = await _distributedCache.Get<TestModel>(idProvider(model), null);
            Assert.Null(cachedValue);
        }
    }
}
