using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Common.Models;

public class PagedList<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 200) pageSize = 200;

        var total = await source.CountAsync(ct);
        var items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedList<T>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}

public class PaginationParams
{
    private int _pageSize = 10;

    public int Page { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 200 ? 200 : value < 1 ? 10 : value;
    }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = true;
}
