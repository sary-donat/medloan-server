using System.ComponentModel.DataAnnotations;

namespace MedLoan.Application.Items;

public sealed record ItemQueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    public int? CategoryId { get; init; }
    public string? Search { get; init; }
    public bool AvailableOnly { get; init; }
    public ItemSortBy SortBy { get; init; } = ItemSortBy.Name;
    public SortDirection SortDir { get; init; } = SortDirection.Asc;
}
