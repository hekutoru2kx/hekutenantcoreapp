namespace Hekutenantcoreapp.Domain.Models;

public abstract class PagedQuery
{
    //Sync with pagination.ts in frontend
    private int _pageSize = 25;
    private int _page = 1;//Page setter clamps to minimum 1 — no negative pages

    public const int MaxPageSize = 100;//PageSize setter clamps between 1 and MaxPageSize — prevents someone requesting 100,000 records

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? 1 : value;
    }

    public string? SortDirection { get; set; } = "asc";
    public string? Search { get; set; }
}