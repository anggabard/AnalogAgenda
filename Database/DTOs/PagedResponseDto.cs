namespace Database.DTOs;

public class PagedResponseDto<T>
{
    public IEnumerable<T> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public bool HasNextPage => (CurrentPage * PageSize) < TotalCount;
    public bool HasPreviousPage => CurrentPage > 1;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
