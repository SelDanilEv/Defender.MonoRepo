namespace Defender.CarService.Tests.Domain;

internal sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
