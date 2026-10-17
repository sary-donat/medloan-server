using System.ComponentModel.DataAnnotations;

namespace MedLoan.Application.Categories;

public sealed record CreateCategoryRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}
