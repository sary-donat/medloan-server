using System.ComponentModel.DataAnnotations;

namespace MedLoan.Application.Items;

public sealed record CreateItemRequest
{
    [Required, StringLength(150)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [StringLength(100)]
    public string? Manufacturer { get; init; }

    [Range(1, 365)]
    public int MaxLoanDays { get; init; } = 30;

    public bool RequiresDeposit { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? DepositAmount { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }
}
