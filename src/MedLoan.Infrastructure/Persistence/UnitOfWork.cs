using MedLoan.Application.Common.Exceptions;
using MedLoan.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    private readonly AppDbContext _db = db;

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The entity was modified by another request.", ex);
        }
    }
}
