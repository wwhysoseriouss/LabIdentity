namespace LabIdentity.ViewModels;

public class PagedListViewModel<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public required PaginationViewModel Pagination { get; init; }

    public string? Search { get; init; }
}