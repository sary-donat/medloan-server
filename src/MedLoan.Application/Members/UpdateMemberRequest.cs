using MedLoan.Domain.Enums;

namespace MedLoan.Application.Members;

// PATCH: only the fields that were sent (not null) are updated.
public sealed record UpdateMemberRequest
{
    public bool? IsActive { get; init; }
    public UserRole? Role { get; init; }
}
