using MedLoan.Application.Common.Interfaces;
using MedLoan.Domain.Entities;
using MedLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Repositories;

public class MemberRepository(AppDbContext db) : IMemberRepository
{
    private readonly AppDbContext _db = db;

    public Task<Member?> GetByEmailAsync(string email, CancellationToken ct) =>
        _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Email == email, ct);
}
