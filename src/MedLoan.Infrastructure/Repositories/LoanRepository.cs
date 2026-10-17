using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Application.Loans;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;
using MedLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Repositories;

public class LoanRepository(AppDbContext db) : ILoanRepository
{
    private readonly AppDbContext _db = db;

    public void Add(Loan loan) => _db.Loans.Add(loan);

    public Task<Loan?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Loans
            .AsNoTracking()
            .Include(l => l.ItemCopy).ThenInclude(c => c.Item)
            .Include(l => l.Member)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    // Tracked, with ItemCopy: the return updates the copy under its concurrency token.
    public Task<Loan?> GetByIdForUpdateAsync(int id, CancellationToken ct) =>
        _db.Loans.Include(l => l.ItemCopy).FirstOrDefaultAsync(l => l.Id == id, ct);

    // Not paged: a member has few loans. Newest first; ThenBy(Id) keeps ties in a stable order.
    public async Task<IReadOnlyList<Loan>> GetByMemberAsync(int memberId, CancellationToken ct) =>
        await _db.Loans
            .AsNoTracking()
            .Include(l => l.ItemCopy).ThenInclude(c => c.Item)
            .Include(l => l.Member)
            .Where(l => l.MemberId == memberId)
            .OrderByDescending(l => l.LoanedAt)
            .ThenBy(l => l.Id)
            .ToListAsync(ct);

    public async Task<PagedResult<Loan>> GetPagedAsync(LoanQueryParameters query, DateTime now, CancellationToken ct)
    {
        var loans = _db.Loans.AsNoTracking();

        if (query.MemberId is int memberId)
            loans = loans.Where(l => l.MemberId == memberId);

        if (query.Status is LoanStatus status)
            loans = loans.Where(l => l.Status == status);

        if (query.OverdueOnly)
            loans = loans.Where(l => l.Status == LoanStatus.Active && l.DueAt < now);

        var totalCount = await loans.CountAsync(ct);

        // Computed in long: (page - 1) * PageSize overflows int for very large pages. Clamping keeps the
        // offset past any real row, so the page is simply empty.
        var skip = (int)Math.Min((long)(query.Page - 1) * PagingDefaults.PageSize, int.MaxValue);

        var page = await loans
            .Include(l => l.ItemCopy).ThenInclude(c => c.Item)
            .Include(l => l.Member)
            .OrderByDescending(l => l.LoanedAt)
            .ThenBy(l => l.Id)
            .Skip(skip)
            .Take(PagingDefaults.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Loan>(page, query.Page, PagingDefaults.PageSize, totalCount);
    }

    public Task<int> CountActiveByMemberAsync(int memberId, CancellationToken ct) =>
        _db.Loans.CountAsync(l => l.MemberId == memberId && l.Status == LoanStatus.Active, ct);

    // Returned loans count too: DeleteBehavior.Restrict blocks deleting any copy that has loans.
    public Task<bool> AnyLoansForItemAsync(int itemId, CancellationToken ct) =>
        _db.Loans.AnyAsync(l => l.ItemCopy.ItemId == itemId, ct);
}
