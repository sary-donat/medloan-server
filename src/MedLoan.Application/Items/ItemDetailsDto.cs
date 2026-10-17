using MedLoan.Application.Copies;
using MedLoan.Application.Tags;

namespace MedLoan.Application.Items;

public sealed record ItemDetailsDto(
    int Id,
    string Name,
    string? Description,
    string? Manufacturer,
    int MaxLoanDays,
    bool RequiresDeposit,
    decimal? DepositAmount,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAt,
    int AvailableCopies,
    IReadOnlyList<CopyDto> Copies,
    IReadOnlyList<TagDto> Tags);
