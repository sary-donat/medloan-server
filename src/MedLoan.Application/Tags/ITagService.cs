using MedLoan.Application.Common.Models;

namespace MedLoan.Application.Tags;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct);
    Task<Result<TagDto>> CreateAsync(CreateTagRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
