namespace ApexProp.Domain.Models;

/// <summary>
/// PagedRequest - בקשה לעמוד מסוים
/// </summary>
public class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool Ascending { get; set; } = true;

    public int GetSkip() => (PageNumber - 1) * PageSize;
}