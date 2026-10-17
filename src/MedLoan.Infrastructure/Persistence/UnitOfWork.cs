using MedLoan.Application.Common.Exceptions;
using MedLoan.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        // After the concurrency catch: DbUpdateConcurrencyException derives from DbUpdateException.
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateValueException("A unique value already exists.", ex);
        }
    }
}
