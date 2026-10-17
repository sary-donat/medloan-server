using MedLoan.Domain.Enums;

namespace MedLoan.Application.Members;

// No PasswordHash: it never leaves the server.
public sealed record MemberDto(
    int Id,
    string FullName,
    string Email,
    string Phone,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);
