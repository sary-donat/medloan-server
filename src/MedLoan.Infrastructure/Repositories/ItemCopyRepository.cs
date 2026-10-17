using MedLoan.Application.Common.Interfaces;
using MedLoan.Domain.Entities;
using MedLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Repositories;

public class ItemCopyRepository(AppDbContext db) : IItemCopyRepository
{
    private readonly AppDbContext _db = db;

    // Tracked on purpose: the copy is modified and saved with its concurrency token.
    public Task<ItemCopy?> GetByIdForUpdateAsync(int id, CancellationToken ct) =>
        _db.ItemCopies.Include(c => c.Item).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<ItemCopy>> GetByItemIdAsync(int itemId, CancellationToken ct) =>
        await _db.ItemCopies.AsNoTracking().Where(c => c.ItemId == itemId).OrderBy(c => c.Id).ToListAsync(ct);

    public Task<bool> SerialNumberExistsAsync(string serialNumber, CancellationToken ct) =>
        _db.ItemCopies.AnyAsync(c => c.SerialNumber == serialNumber, ct);

    public void Add(ItemCopy copy) => _db.ItemCopies.Add(copy);

    public void Remove(ItemCopy copy) => _db.ItemCopies.Remove(copy);
}
