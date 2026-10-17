namespace MedLoan.Application.Items;

public sealed record ItemSummaryDto(
    int Id,
    string Name,
    string? Manufacturer,
    int CategoryId,
    string CategoryName,
    bool RequiresDeposit,
    int AvailableCopies);
