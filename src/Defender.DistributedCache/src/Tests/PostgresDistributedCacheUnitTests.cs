using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using Defender.DistributedCache.Configuration.Options;
using Defender.DistributedCache.Postgres;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Defender.DistributedCache.Tests;

public class PostgresDistributedCacheUnitTests
{
    private static readonly DistributedCacheOptions Options = new()
    {
        ConnectionString = "Host=127.0.0.1;Port=1;Database=cache_database;Username=postgres;Password=postgres;Timeout=1;Command Timeout=1;Pooling=false",
        CacheTableName = "cache",
        TtlForCacheEntriesSeconds = 30
    };

    [Fact]
    public async Task Add_WhenConnectionIsNotEstablished_DoesNotThrow()
    {
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() =>
            sut.Add(model => model.Name, new TestModel { Name = "alpha", Age = 20 }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetByKey_WhenConnectionIsNotEstablishedAndFetchIsMissing_ReturnsNull()
    {
        var sut = CreateSut();

        var result = await sut.Get<TestModel>("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByKey_WhenConnectionIsNotEstablishedAndFetchIsProvided_ReturnsFetchedValue()
    {
        var sut = CreateSut();
        var expected = new TestModel { Name = "beta", Age = 22 };

        var result = await sut.Get("key", () => Task.FromResult(expected));

        Assert.NotNull(result);
        Assert.Equal(expected.Name, result.Name);
        Assert.Equal(expected.Age, result.Age);
    }

    [Fact]
    public async Task GetByExpressions_WhenConnectionIsNotEstablishedAndFetchIsProvided_ReturnsFetchedValue()
    {
        var sut = CreateSut();
        var expected = new TestModel { Name = "gamma", Age = 33 };
        var expressions = new List<Expression<Func<TestModel, bool>>> { x => x.Name == expected.Name };

        var result = await sut.Get(expressions, x => x.Name, () => Task.FromResult(expected));

        Assert.NotNull(result);
        Assert.Equal(expected.Name, result.Name);
        Assert.Equal(expected.Age, result.Age);
    }

    [Fact]
    public async Task Invalidate_WhenConnectionIsNotEstablished_DoesNotThrow()
    {
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() => sut.Invalidate("key"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task InvalidateByExpressions_WhenConnectionIsNotEstablished_DoesNotThrow()
    {
        var sut = CreateSut();
        var expressions = new List<Expression<Func<TestModel, bool>>> { x => x.Age == 20 };

        var exception = await Record.ExceptionAsync(() => sut.Invalidate(expressions));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Add_WhenConnectionFlagIsForcedTrue_ExecutesConnectedPathWithoutThrowing()
    {
        var sut = CreateSut();
        SetConnectionEstablished(sut, true);

        var exception = await Record.ExceptionAsync(() =>
            sut.Add(model => model.Name, new TestModel { Name = "epsilon", Age = 30 }, null));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetByKey_WhenConnectionFlagIsForcedTrueAndNoFallback_ReturnsNull()
    {
        var sut = CreateSut();
        SetConnectionEstablished(sut, true);

        var result = await sut.Get<TestModel>("missing-key", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByKey_WhenConnectionFlagIsForcedTrueAndFallbackProvided_ReturnsFetchedValue()
    {
        var sut = CreateSut();
        SetConnectionEstablished(sut, true);
        var expected = new TestModel { Name = "zeta", Age = 31 };

        var result = await sut.Get("missing-key", () => Task.FromResult(expected), TimeSpan.FromSeconds(3));

        Assert.NotNull(result);
        Assert.Equal(expected.Name, result.Name);
        Assert.Equal(expected.Age, result.Age);
    }

    [Fact]
    public async Task GetByExpressions_WhenConnectionFlagIsForcedTrueAndFallbackMissing_ReturnsNull()
    {
        var sut = CreateSut();
        SetConnectionEstablished(sut, true);
        var expressions = new List<Expression<Func<TestModel, bool>>> { x => x.Age == 10 };

        var result = await sut.Get<TestModel>(expressions);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByExpressions_WhenConnectionFlagIsForcedTrueAndFallbackProvided_ReturnsFetchedValue()
    {
        var sut = CreateSut();
        SetConnectionEstablished(sut, true);
        var expected = new TestModel { Name = "eta", Age = 44 };
        var expressions = new List<Expression<Func<TestModel, bool>>> { x => x.Name == expected.Name };

        var result = await sut.Get(expressions, model => model.Name, () => Task.FromResult(expected), TimeSpan.FromSeconds(2));

        Assert.NotNull(result);
        Assert.Equal(expected.Name, result.Name);
        Assert.Equal(expected.Age, result.Age);
    }

    [Fact]
    public async Task InvalidateByExpressions_WhenConnectionFlagIsForcedTrue_DoesNotThrow()
    {
        var sut = CreateSut();
        SetConnectionEstablished(sut, true);
        var expressions = new List<Expression<Func<TestModel, bool>>> { x => x.Name == "invalidate" };

        var exception = await Record.ExceptionAsync(() => sut.Invalidate(expressions));

        Assert.Null(exception);
    }

    [Fact]
    public void CheckIfConnectionEstablished_WhenFlagIsTrue_ReturnsTrue()
    {
        var sut = CreateSut();
        var flagProperty = typeof(PostgresDistributedCache)
            .GetProperty("IsConnectionEstablished", BindingFlags.Instance | BindingFlags.NonPublic)!;
        flagProperty.SetValue(sut, true);

        var method = typeof(PostgresDistributedCache)
            .GetMethod("CheckIfConnectionEstablished", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var result = (bool)method.Invoke(sut, Array.Empty<object>())!;

        Assert.True(result);
    }

    [Fact]
    public void ParseExpressions_WhenBinaryExpressionsProvided_ReturnsSqlConditionsAndParameters()
    {
        var expressions = new List<Expression<Func<TestModel, bool>>>
        {
            x => x.Name == "john",
            x => x.Age == 40
        };

        var parseMethod = typeof(PostgresDistributedCache)
            .GetMethod("ParseExpressions", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TestModel));

        var (conditions, parameters) =
            ((List<string>, DynamicParameters))parseMethod.Invoke(null, new object[] { expressions })!;

        Assert.Equal(2, conditions.Count);
        Assert.All(conditions, condition => Assert.Contains("jsonb_build_object", condition));
        Assert.Contains("Name", parameters.ParameterNames);
        Assert.Contains("Age", parameters.ParameterNames);
    }

    [Fact]
    public void ParseExpressions_WhenUnsupportedExpressionProvided_ThrowsNotSupportedException()
    {
        var expressions = new List<Expression<Func<TestModel, bool>>>
        {
            x => x.Name.StartsWith("j")
        };

        var parseMethod = typeof(PostgresDistributedCache)
            .GetMethod("ParseExpressions", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TestModel));

        var exception = Assert.Throws<TargetInvocationException>(() =>
            parseMethod.Invoke(null, new object[] { expressions }));

        Assert.IsType<NotSupportedException>(exception.InnerException);
    }

    [Fact]
    public void GetMemberExpression_WhenUnaryExpressionProvided_ReturnsInnerMember()
    {
        Expression<Func<TestModel, object>> expression = x => x.Age;

        var method = typeof(PostgresDistributedCache)
            .GetMethod("GetMemberExpression", BindingFlags.NonPublic | BindingFlags.Static)!;

        var memberExpression = (MemberExpression?)method.Invoke(null, new object[] { expression.Body });

        Assert.NotNull(memberExpression);
        Assert.Equal("Age", memberExpression.Member.Name);
    }

    [Fact]
    public void GetExpressionValue_WhenConstantMemberAndMethodCallProvided_ReturnsValues()
    {
        const int expectedAge = 27;
        var data = new TestModel { Name = "delta", Age = expectedAge };
        Expression<Func<TestModel>> memberLambda = () => data;
        Expression<Func<string>> methodCallLambda = () => data.Name.ToUpperInvariant();

        var method = typeof(PostgresDistributedCache)
            .GetMethod("GetExpressionValue", BindingFlags.NonPublic | BindingFlags.Static)!;

        var constantValue = method.Invoke(null, new object[] { Expression.Constant(42) });
        var memberValue = (TestModel?)method.Invoke(null, new object[] { memberLambda.Body });
        var methodCallValue = (string?)method.Invoke(null, new object[] { methodCallLambda.Body });

        Assert.Equal(42, constantValue);
        Assert.NotNull(memberValue);
        Assert.Equal(expectedAge, memberValue.Age);
        Assert.Equal("DELTA", methodCallValue);
    }

    [Fact]
    public void GetExpressionValue_WhenUnsupportedExpressionProvided_ThrowsNotSupportedException()
    {
        var expression = Expression.New(typeof(TestModel));

        var method = typeof(PostgresDistributedCache)
            .GetMethod("GetExpressionValue", BindingFlags.NonPublic | BindingFlags.Static)!;

        var exception = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, new object[] { expression }));

        Assert.IsType<NotSupportedException>(exception.InnerException);
    }

    private static PostgresDistributedCache CreateSut()
    {
        return new PostgresDistributedCache(
            Microsoft.Extensions.Options.Options.Create(Options),
            new Mock<ILogger<PostgresDistributedCache>>().Object);
    }

    private static void SetConnectionEstablished(PostgresDistributedCache cache, bool value)
    {
        var flagProperty = typeof(PostgresDistributedCache)
            .GetProperty("IsConnectionEstablished", BindingFlags.Instance | BindingFlags.NonPublic)!;
        flagProperty.SetValue(cache, value);
    }
}
