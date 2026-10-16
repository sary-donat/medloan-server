using MedLoan.Domain.Common;
using MedLoan.Domain.Enums;

namespace MedLoan.Domain.Entities;

public class Loan : BaseEntity
{
    public int ItemCopyId { get; set; }
    public ItemCopy ItemCopy { get; set; } = null!;
    public int MemberId { get; set; }
    public DateTime LoanedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public LoanStatus Status { get; set; }
}
