using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Application.Members;
using MedLoan.Domain.Entities;
using MedLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Repositories;

public class MemberRepository(AppDbContext db) : IMemberRepository
{
    private readonly AppDbContext _db = db;

    public Task<Member?> GetByEmailAsync(string email, CancellationToken ct) =>
        _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Email == email, ct);

    public Task<Member?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<Member?> GetByIdForUpdateAsync(int id, CancellationToken ct) =>
        _db.Members.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<PagedResult<MemberDto>> GetPagedAsync(int page, CancellationToken ct)
    {
        var members = _db.Members.AsNoTracking();

        var totalCount = await members.CountAsync(ct);

        // Computed in long: (page - 1) * PageSize overflows int for very large pages. Clamping keeps the
        // offset past any real row, so the page is simply empty.
        var skip = (int)Math.Min((long)(page - 1) * PagingDefaults.PageSize, int.MaxValue);

        // Projected in the query, so PasswordHash is never read.
        var items = await members
            .OrderBy(m => m.Id)
            .Skip(skip)
            .Take(PagingDefaults.PageSize)
            .Select(m => new MemberDto(m.Id, m.FullName, m.Email, m.Phone, m.Role, m.IsActive, m.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<MemberDto>(items, page, PagingDefaults.PageSize, totalCount);
    }

    public void Add(Member member) => _db.Members.Add(member);
}
