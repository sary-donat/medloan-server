using MedLoan.Domain.Enums;

namespace MedLoan.Application.Loans;

public sealed record LoanDto(
    int Id,
    int ItemCopyId,
    string SerialNumber,
    int ItemId,
    string ItemName,
    int MemberId,
    string MemberFullName,
    DateTime LoanedAt,
    DateTime DueAt,
    DateTime? ReturnedAt,
    LoanStatus Status);
