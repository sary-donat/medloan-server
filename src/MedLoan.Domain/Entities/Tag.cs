using MedLoan.Domain.Common;

namespace MedLoan.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
