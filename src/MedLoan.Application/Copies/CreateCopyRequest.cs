using System.ComponentModel.DataAnnotations;
using MedLoan.Domain.Enums;

namespace MedLoan.Application.Copies;

public sealed record CreateCopyRequest
{
    [Required, StringLength(50)]
    public string SerialNumber { get; init; } = string.Empty;

    public CopyCondition Condition { get; init; }
    public string? Notes { get; init; }
}
