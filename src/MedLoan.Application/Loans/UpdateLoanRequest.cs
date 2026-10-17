using System.ComponentModel.DataAnnotations;

namespace MedLoan.Application.Loans;

// Nullable so that [Required] rejects a missing field. Only true is allowed (checked in LoanService).
public sealed record UpdateLoanRequest
{
    [Required]
    public bool? Returned { get; init; }
}
