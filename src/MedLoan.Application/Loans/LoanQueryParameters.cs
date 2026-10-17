using System.ComponentModel.DataAnnotations;
using MedLoan.Domain.Enums;

namespace MedLoan.Application.Loans;

public sealed record LoanQueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    public int? MemberId { get; init; }
    public LoanStatus? Status { get; init; }
    public bool OverdueOnly { get; init; }
}
