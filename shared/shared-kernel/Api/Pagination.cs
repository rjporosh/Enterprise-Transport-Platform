namespace Platform.SharedKernel.Api;

/// <summary>
/// Standard paged-request inputs. Enforces an upper bound on page size so a
/// caller can never ask for an unbounded result set (.ai/MASTER-RULES.md §51).
/// </summary>
public sealed record PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public int Page { get; init; } = DefaultPage;
    public int PageSize { get; init; } = DefaultPageSize;
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Asc;

    /// <summary>Returns a copy with page/size clamped to valid bounds.</summary>
    public PageRequest Normalized() => this with
    {
        Page = Page < 1 ? DefaultPage : Page,
        PageSize = PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => PageSize
        }
    };

    public int Skip => (Math.Max(Page, 1) - 1) * Math.Clamp(PageSize, 1, MaxPageSize);
}

public enum SortDirection
{
    Asc = 0,
    Desc = 1
}

/// <summary>Standard paged-response envelope.</summary>
public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required long TotalItems { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;

    public static PagedResult<T> Create(IReadOnlyList<T> items, PageRequest request, long totalItems) => new()
    {
        Items = items,
        Page = request.Page,
        PageSize = request.PageSize,
        TotalItems = totalItems
    };
}
