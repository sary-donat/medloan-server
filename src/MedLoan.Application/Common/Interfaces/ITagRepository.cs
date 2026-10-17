using MedLoan.Domain.Entities;

namespace MedLoan.Application.Common.Interfaces;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<Tag>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
    Task<Tag?> GetByIdForUpdateAsync(int id, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, CancellationToken ct);
    void Add(Tag tag);
    void Remove(Tag tag);
}
