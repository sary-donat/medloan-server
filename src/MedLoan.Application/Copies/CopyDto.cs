using MedLoan.Domain.Enums;

namespace MedLoan.Application.Copies;

public sealed record CopyDto(int Id, string SerialNumber, CopyStatus Status, CopyCondition Condition, string? Notes);
