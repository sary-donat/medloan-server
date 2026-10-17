using MedLoan.Application.Common.Interfaces;
using MedLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Repositories;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    private readonly AppDbContext _db = db;

    public Task<bool> NameExistsAsync(string name, CancellationToken ct) =>
        _db.Categories.AnyAsync(c => c.Name == name, ct);
}
