using MedLoan.Domain.Common;

namespace MedLoan.Domain.Entities;

public class Item : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public int MaxLoanDays { get; set; } = 30;
    public bool RequiresDeposit { get; set; }
    public decimal? DepositAmount { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<ItemCopy> Copies { get; set; } = new List<ItemCopy>();
    public DateTime CreatedAt { get; set; }
}
