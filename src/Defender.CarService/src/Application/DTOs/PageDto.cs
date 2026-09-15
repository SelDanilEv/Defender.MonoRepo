namespace Defender.CarService.Application.DTOs;

public class PageDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int TotalItemsCount { get; init; }

    public int CurrentPage { get; init; }

    public int PageSize { get; init; }

    public int TotalPagesCount { get; init; }
}
