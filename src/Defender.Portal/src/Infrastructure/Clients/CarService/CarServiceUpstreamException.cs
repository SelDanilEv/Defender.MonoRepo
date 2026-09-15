namespace Defender.Portal.Infrastructure.Clients.CarService;

public sealed class CarServiceUpstreamException(int status, string? code, string detail) : Exception(detail)
{
    public int Status { get; } = status;

    public string? Code { get; } = code;

    public string Detail { get; } = detail;
}
