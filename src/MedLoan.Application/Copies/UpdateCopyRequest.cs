using MedLoan.Domain.Enums;

namespace MedLoan.Application.Copies;

// PATCH: only the fields that were sent (not null) are updated.
public sealed record UpdateCopyRequest
{
    public CopyStatus? Status { get; init; }
    public CopyCondition? Condition { get; init; }
    public string? Notes { get; init; }
}
