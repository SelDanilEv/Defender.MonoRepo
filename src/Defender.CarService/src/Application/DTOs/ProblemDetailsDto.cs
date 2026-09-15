namespace Defender.CarService.Application.DTOs;

public sealed class ProblemDetailsDto
{
    public string? Type { get; init; }

    public string? Title { get; init; }

    public int? Status { get; init; }

    public string? Detail { get; init; }

    public string? Instance { get; init; }

    public string? Code { get; init; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public string? TraceId { get; init; }
}
