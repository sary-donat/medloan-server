using MedLoan.Domain.Common;

namespace MedLoan.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
