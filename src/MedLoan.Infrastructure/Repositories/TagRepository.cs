using MedLoan.Application.Common.Interfaces;
using MedLoan.Domain.Entities;
using MedLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Repositories;

public class TagRepository(AppDbContext db) : ITagRepository
{
    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken ct) =>
        await _db.Tags.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);

    // Tracked on purpose: the tags are attached to an item that is being saved.
    public async Task<IReadOnlyList<Tag>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        await _db.Tags.Where(t => ids.Contains(t.Id)).ToListAsync(ct);

    public Task<Tag?> GetByIdForUpdateAsync(int id, CancellationToken ct) =>
        _db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, CancellationToken ct) =>
        _db.Tags.AnyAsync(t => t.Name == name, ct);

    public void Add(Tag tag) => _db.Tags.Add(tag);

    public void Remove(Tag tag) => _db.Tags.Remove(tag);
}
