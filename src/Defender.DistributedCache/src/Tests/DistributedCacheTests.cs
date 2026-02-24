using Defender.DistributedCache.Configuration.Options;
using Defender.DistributedCache.Postgres;
using Defender.DistributedCacheTestWebApi.Model;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq.Expressions;
using Defender.DistributedCache.Postgres.TTL;
using Microsoft.Extensions.Logging;

namespace Defender.DistributedCache.Tests
{
    public class DistributedCacheTests
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IPostgresCacheCleanupService _postgresCacheCleanupService;

        public DistributedCacheTests()
        {
            var options = Options.Create(new DistributedCacheOptions 
            {
                ConnectionString = "Host=host.docker.internal;Port=5432;Database=cache_database;Username=postgres;Password=postgres",
                CacheTableName = "cache"
            });

            var logger = new Mock<ILogger<PostgresDistributedCache>>().Object;
            _distributedCache = new PostgresDistributedCache(options, logger);
            _postgresCacheCleanupService = new PostgresCacheCleanupService(options);

            _postgresCacheCleanupService.CheckAndRunCleanupAsync();
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
