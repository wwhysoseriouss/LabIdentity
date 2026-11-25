namespace LabIdentity.ViewModels;

public class PaginationViewModel
{
    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages { get; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public PaginationViewModel(
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        TotalCount = totalCount;
        PageSize = pageSize;

        TotalPages = Math.Max(
            1,
            (int)Math.Ceiling(totalCount / (double)pageSize));

        PageNumber = Math.Clamp(pageNumber, 1, TotalPages);
    }
}