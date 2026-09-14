namespace Defender.Portal.Application.Configuration.Options;

public sealed class CarServiceOptions
{
    public string Url { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
}
