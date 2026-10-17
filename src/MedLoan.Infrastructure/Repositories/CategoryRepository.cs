using MedLoan.Application.Common.Interfaces;
using MedLoan.Domain.Entities;
using MedLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Repositories;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct) =>
        await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Category?> GetByIdForUpdateAsync(int id, CancellationToken ct) =>
        _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct) =>
        _db.Categories.AnyAsync(c => c.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct) =>
        _db.Categories.AnyAsync(c => c.Name == name && c.Id != excludeId, ct);

    public void Add(Category category) => _db.Categories.Add(category);

    public void Remove(Category category) => _db.Categories.Remove(category);
}
