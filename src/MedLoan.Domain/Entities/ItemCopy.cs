using MedLoan.Domain.Common;
using MedLoan.Domain.Enums;

namespace MedLoan.Domain.Entities;

public class ItemCopy : BaseEntity
{
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public string SerialNumber { get; set; } = string.Empty;
    public CopyStatus Status { get; set; }
    public CopyCondition Condition { get; set; }
    public string? Notes { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();

    public bool IsAvailable => Status == CopyStatus.Available;

    public void CheckOut()
    {
        if (!IsAvailable) throw new DomainException("העותק אינו זמין");
        Status = CopyStatus.Loaned;
    }

    public void CheckIn()
    {
        if (Status != CopyStatus.Loaned) throw new DomainException("העותק אינו מושאל");
        Status = CopyStatus.Available;
    }
}
