using System.ComponentModel.DataAnnotations;

namespace MedLoan.Application.Loans;

// No MemberId: the borrower is always the logged-in member.
public sealed record CreateLoanRequest
{
    [Range(1, int.MaxValue)]
    public int ItemCopyId { get; init; }
}
