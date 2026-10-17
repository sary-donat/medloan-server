using MedLoan.Application.Common.Models;

namespace MedLoan.Application.Copies;

public interface ICopyService
{
    Task<Result<IReadOnlyList<CopyDto>>> GetByItemIdAsync(int itemId, CancellationToken ct);
    Task<Result<CopyDto>> CreateAsync(int itemId, CreateCopyRequest request, CancellationToken ct);
    Task<Result> UpdateAsync(int id, UpdateCopyRequest request, CancellationToken ct);
}
